using System.Threading.Tasks;

namespace DataForgeETL.UI.Services
{
    /// <summary>
    /// Abstraction for user input that works in both console and UI modes
    /// </summary>
    public interface IUserInputService
    {
        /// <summary>
        /// Gets text input from user with validation
        /// </summary>
        Task<string?> GetTextInputAsync(string prompt, string defaultValue = "", bool required = true);

        /// <summary>
        /// Gets yes/no confirmation from user
        /// </summary>
        Task<bool> GetYesNoInputAsync(string prompt, bool defaultValue = false);

        /// <summary>
        /// Gets table name with SQL validation
        /// </summary>
        Task<string?> GetTableNameAsync(string prompt, bool validateSQL = true);

        /// <summary>
        /// Displays a message to the user
        /// </summary>
        Task ShowMessageAsync(string title, string message, MessageType messageType = MessageType.Information);

        /// <summary>
        /// Displays an error to the user
        /// </summary>
        Task ShowErrorAsync(string title, string message);

        /// <summary>
        /// Displays a confirmation dialog
        /// </summary>
        Task<bool> ShowConfirmationAsync(string title, string message);
    }

    /// <summary>
    /// Message types for user notifications
    /// </summary>
    public enum MessageType
    {
        Information,
        Success,
        Warning,
        Error
    }
}
