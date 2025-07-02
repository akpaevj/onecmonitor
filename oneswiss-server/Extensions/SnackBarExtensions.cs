using MudBlazor;

namespace OneSwiss.Server.Extensions;

public static class SnackBarExtensions
{
    private static void ShowMessage(this ISnackbar snackbar, string message, Severity severity = Severity.Info)
    {
        snackbar.Add(message, severity, options =>
        {
            options.CloseAfterNavigation = true;
        });
    }
    
    public static void ShowSuccess(this ISnackbar snackbar, string message)
    {
        snackbar.ShowMessage(message, Severity.Success);
    }
    
    public static void ShowError(this ISnackbar snackbar, string message)
    {
        snackbar.ShowMessage(message, Severity.Error);
    }
    
    public static void ShowError(this ISnackbar snackbar, Exception exception)
        =>  snackbar.ShowError(exception.Message);
}