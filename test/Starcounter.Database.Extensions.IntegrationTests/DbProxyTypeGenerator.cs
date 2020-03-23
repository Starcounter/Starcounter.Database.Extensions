using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    class DbProxyTypeGenerator
    {
        readonly ModuleBuilder _moduleBuilder;
        Dictionary<Type, Type> _proxyMap = new Dictionary<Type, Type>();

        public DbProxyTypeGenerator() => _moduleBuilder = CreateBuilder();

        public Type EnsureProxyType(Type userDefinedType)
        {
            if (!_proxyMap.TryGetValue(userDefinedType, out Type proxyType))
            {
                proxyType = GenerateProxyType(userDefinedType);
                _proxyMap.Add(userDefinedType, proxyType);
            }

            return proxyType;
        }

        Type GenerateProxyType(Type userDefinedType)
        {
            var typeBuilder = _moduleBuilder.DefineType(
                _moduleBuilder.Assembly.GetName() + "." + userDefinedType.Name + "_Impl",
                TypeAttributes.Public | TypeAttributes.Sealed,
                userDefinedType);

            return typeBuilder.CreateTypeInfo().AsType();
        }

        ModuleBuilder CreateBuilder()
        {
            var thisAssemblyName = typeof(DbProxyTypeGenerator).Assembly.GetName().Name;
            var dynamicAssemblyFullName = $"{thisAssemblyName}.GeneratedImplementations";
            var assemblyName = new AssemblyName(dynamicAssemblyFullName);
            return AssemblyBuilder
                .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(assemblyName.FullName);
        }
    }
}
