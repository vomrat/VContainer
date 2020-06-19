using System;

namespace VContainer.Internal
{
    sealed class ReflectionInjector : IInjector
    {
        readonly InjectTypeInfo injectTypeInfo;

        public ReflectionInjector(InjectTypeInfo injectTypeInfo)
        {
            this.injectTypeInfo = injectTypeInfo;
        }

        public void Inject(object instance, IObjectResolver resolver)
        {
            InjectMethods(instance, resolver);
            InjectProperties(instance, resolver);
            InjectFields(instance, resolver);
        }

        public object CreateInstance(IObjectResolver resolver)
        {
            var parameters = injectTypeInfo.InjectConstructor.ParameterInfos;
            var parameterValues = CappedArrayPool<object>.Shared8Limit.Rent(parameters.Length);
            try
            {
                for (var i = 0; i < parameters.Length; i++)
                {
                    try
                    {
                        parameterValues[i] = resolver.Resolve(parameters[i].ParameterType);
                    }
                    catch (VContainerException ex)
                    {
                        throw new VContainerException(injectTypeInfo.Type, $"Failed to resolve {injectTypeInfo.Type.FullName} : {ex.Message}");
                    }
                }
                var instance = injectTypeInfo.InjectConstructor.ConstructorInfo.Invoke(parameterValues);
                Inject(instance, resolver);
                return instance;
            }
            finally
            {
                CappedArrayPool<object>.Shared8Limit.Return(parameterValues);
            }
        }

        void InjectFields(object obj, IObjectResolver resolver)
        {
            foreach (var f in injectTypeInfo.InjectFields)
            {
                // if (f.GetValue())
                {
                    var fieldValue = resolver.Resolve(f.FieldInfo.FieldType);
                    f.FieldInfo.SetValue(obj, fieldValue);
                }
            }
        }

        void InjectProperties(object obj, IObjectResolver resolver)
        {
            foreach (var prop in injectTypeInfo.InjectProperties)
            {
                var propValue = resolver.Resolve(prop.PropertyInfo.PropertyType);
                prop.PropertyInfo.SetValue(obj, propValue);
            }
        }

        void InjectMethods(object obj, IObjectResolver resolver)
        {
            foreach (var method in injectTypeInfo.InjectMethods)
            {
                var parameters = method.ParameterInfos;
                var parameterValues = CappedArrayPool<object>.Shared8Limit.Rent(parameters.Length);
                try
                {
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameterType = parameters[i].ParameterType;
                        try
                        {
                            parameterValues[i] = resolver.Resolve(parameterType);
                        }
                        catch (VContainerException ex)
                        {
                            throw new VContainerException(parameterType, $"Failed to resolve {injectTypeInfo.Type.FullName} : {ex.Message}");
                        }
                    }
                    method.MethodInfo.Invoke(obj, parameterValues);
                }
                finally
                {
                    CappedArrayPool<object>.Shared8Limit.Return(parameterValues);
                }
            }
        }
    }

    sealed class ReflectionInjectorBuilder : IInjectorBuilder
    {
        public static readonly ReflectionInjectorBuilder Default = new ReflectionInjectorBuilder();

        public IInjector Build(Type type)
        {
            var injectTypeInfo = TypeAnalyzer.AnalyzeWithCache(type);
            return new ReflectionInjector(injectTypeInfo);
       }
    }
}