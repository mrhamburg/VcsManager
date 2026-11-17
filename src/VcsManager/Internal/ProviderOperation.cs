namespace VcsManager.Internal;

internal static class ProviderOperation
{
    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        Func<Exception, Exception> mapFailure,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync<object?>(
            async token =>
            {
                await operation(token);
                return null;
            },
            mapFailure,
            cancellationToken);
    }

    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<Exception, Exception> mapFailure,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (VcsProviderException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw mapFailure(exception);
        }
    }
}
