using MudBlazor;

namespace OneSwiss.Server.Extensions;

public static class SnackBarExtensions
{
    public static void ShowMessage(
        this ISnackbar snackbar, 
        string message, 
        Severity severity = Severity.Info, 
        bool closeAfterNavigation = true)
    {
        snackbar.Add(message, severity, options =>
        {
            options.CloseAfterNavigation = closeAfterNavigation;
        });
    }
    
    public static void ShowSuccess(this ISnackbar snackbar, string message, bool closeAfterNavigation = true)
    {
        snackbar.ShowMessage(message, Severity.Success);
    }
    
    public static void ShowError(this ISnackbar snackbar, string message, bool closeAfterNavigation = true)
    {
        snackbar.ShowMessage(message, Severity.Error);
    }
    
    public static void ShowWarning(this ISnackbar snackbar, string message, bool closeAfterNavigation = true)
    {
        snackbar.ShowMessage(message, Severity.Warning);
    }
    
    public static void ShowError(this ISnackbar snackbar, Exception exception, bool closeAfterNavigation = true)
        =>  snackbar.ShowError(exception.Message);
}