using Castle.DynamicProxy;
using System.Reflection;

namespace RentalCar.Core.Interceptors
{
    public class AspectInterceptorSelector: IInterceptorSelector
    {
        public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
        {
            var classAttributes = type.GetCustomAttributes<MethodInterceptionBaseAttribute>(true).ToList();

            var methodAttributes = method.GetCustomAttributes<MethodInterceptionBaseAttribute>(true).ToList();

            classAttributes.AddRange(methodAttributes);

            return classAttributes.OrderBy(x => x.Priority).ToArray();
        }
    }
}
