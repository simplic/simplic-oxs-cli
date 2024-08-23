using Spectre.Console.Cli;
using System.Reflection;
using System.Reflection.Emit;

namespace Simplic.OxS.CLI.Core
{
    /// <summary>
    /// Dark magic to automatically build a settings class from an interface hierarchy
    /// </summary>
    public class SettingsGenerator
    {
        private readonly AssemblyBuilder assembly;
        private readonly ModuleBuilder module;

        public SettingsGenerator(AssemblyName name)
        {
            assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            module = assembly.DefineDynamicModule(name.Name ?? "Simplic.OxS.CLI.DynamicAssembly");
        }

        public Type Generate(Type settings)
        {
            if (!settings.IsInterface)
                return settings;

            var builder = module.DefineType(settings.FullName + "AutoImpl", TypeAttributes.Public, typeof(CommandSettings), [settings, typeof(IInjectedSettings)]);

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var properties = settings.GetInterfaces().Concat([settings, typeof(IInjectedSettings)]).SelectMany(p => p.GetProperties(flags));
            foreach (var property in properties)
                ImplementProperty(builder, property);

            return builder.CreateType();
        }

        private static void ImplementProperty(TypeBuilder builder, PropertyInfo property)
        {
            var fb = builder.DefineField("m_" + property.Name, property.PropertyType, FieldAttributes.Private);
            var pb = builder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, null);
            var attr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;

            foreach (var attribute in property.CustomAttributes)
            {
                var cargs = attribute.ConstructorArguments.Select(a =>
                {
                    if (a.Value is IEnumerable<CustomAttributeTypedArgument> enumerable)
                    {
                        return enumerable.Select(v => v.Value).ToArray();
                    }
                    return a.Value;
                }).ToArray();
                var pinfo = new List<PropertyInfo>();
                var pargs = new List<object?>();
                var finfo = new List<FieldInfo>();
                var fargs = new List<object?>();
                foreach (var arg in attribute.NamedArguments)
                {
                    if (arg.IsField)
                    {
                        finfo.Add((FieldInfo)arg.MemberInfo);
                        fargs.Add(arg.TypedValue.Value);
                    }
                    else
                    {
                        pinfo.Add((PropertyInfo)arg.MemberInfo);
                        pargs.Add(arg.TypedValue.Value);
                    }
                }
                var ab = new CustomAttributeBuilder(attribute.Constructor, cargs, [.. pinfo], [.. pargs], [.. finfo], [.. fargs]);
                pb.SetCustomAttribute(ab);
            }

            var origGetter = property.GetMethod;
            var origSetter = property.SetMethod;

            if (origGetter != null)
            {
                var getter = builder.DefineMethod(
                    "get_" + property.Name, attr, CallingConventions.HasThis,
                    property.PropertyType,
                    origGetter.ReturnParameter.GetRequiredCustomModifiers(),
                    origGetter.ReturnParameter.GetOptionalCustomModifiers(),
                    null, null, null
                );
                var getterIL = getter.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, fb);
                getterIL.Emit(OpCodes.Ret);
                pb.SetGetMethod(getter);
                builder.DefineMethodOverride(getter, origGetter);
            }

            if (origSetter != null)
            {
                var parameter = origSetter.GetParameters()[0];
                var setter = builder.DefineMethod(
                    "set_" + property.Name, attr, CallingConventions.HasThis,
                    origSetter.ReturnParameter.ParameterType,
                    origSetter.ReturnParameter.GetRequiredCustomModifiers(),
                    origSetter.ReturnParameter.GetOptionalCustomModifiers(),
                    [parameter.ParameterType],
                    [parameter.GetRequiredCustomModifiers()],
                    [parameter.GetOptionalCustomModifiers()]
                );
                var setterIL = setter.GetILGenerator();
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, fb);
                setterIL.Emit(OpCodes.Ret);
                pb.SetSetMethod(setter);
                builder.DefineMethodOverride(setter, origSetter);
            }
        }
    }
}
