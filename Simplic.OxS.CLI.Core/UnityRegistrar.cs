using Spectre.Console.Cli;
using Unity;

namespace Simplic.OxS.CLI.Core
{
    internal class UnityRegistrar(IUnityContainer container) : ITypeRegistrar, ITypeResolver
    {
        public ITypeResolver Build() => this;

        public void Register(Type service, Type implementation) => container.RegisterType(service, implementation);

        public void RegisterInstance(Type service, object implementation) => container.RegisterInstance(service, implementation);

        public void RegisterLazy(Type service, Func<object> factory) => container.RegisterFactory(service, _ => factory());

        public object? Resolve(Type? type) => container.Resolve(type);
    }
}
