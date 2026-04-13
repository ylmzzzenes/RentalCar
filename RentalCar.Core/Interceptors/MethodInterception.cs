using Castle.DynamicProxy;

namespace RentalCar.Core.Interceptors
{
    public abstract class MethodInterception: MethodInterceptionBaseAttribute
    {
        protected virtual void OnBefore(IInvocation invocation)
        {

        }

        protected virtual void onAfter(IInvocation invocation)
        {

        }
        protected virtual void onException(IInvocation invocation, Exception exception)
        {

        }

        protected virtual void onSuccess(IInvocation ınvocation)
        {

        }

        public override void Intercept(IInvocation invocation)
        {
            var isSuccess = true;

            OnBefore(invocation);

            try
            {
                invocation.Proceed();
            }
            catch(Exception ex)
            {
                isSuccess = false;
                onException(invocation, ex);
                throw;
            }
            finally
            {
                if(isSuccess)
                {
                    onSuccess(invocation);
                }

                onAfter(invocation);
            }
        }
    }
}
