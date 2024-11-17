using RHToolkit.Models.MessageBox;
using RHToolkit.Services;

namespace RHToolkit.Models.Database
{
    public class MailDataManager(IDatabaseService databaseService)
    {
        private readonly IDatabaseService _databaseService = databaseService;

        private static readonly char[] separator = [','];

        /// <summary>
        /// Retrieves the list of recipients based on the provided parameters.
        /// </summary>
        /// <param name="mailRecipient">The mail recipient string.</param>
        /// <param name="sendToAllCharacters">Flag to send to all characters.</param>
        /// <param name="sendToAllOnline">Flag to send to all online characters.</param>
        /// <param name="sendToAllOffline">Flag to send to all offline characters.</param>
        /// <returns>An array of recipient names.</returns>
        public async Task<string[]> GetRecipientsAsync(string? mailRecipient, bool sendToAllCharacters, bool sendToAllOnline, bool sendToAllOffline)
        {
            if (sendToAllCharacters)
            {
                return await _databaseService.GetAllCharacterNamesAsync();
            }
            else if (sendToAllOnline)
            {
                return await _databaseService.GetAllCharacterNamesAsync("Y");
            }
            else if (sendToAllOffline)
            {
                return await _databaseService.GetAllCharacterNamesAsync("N");
            }
            else
            {
                string[] recipients = mailRecipient!.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(r => r.Trim())
                                            .ToArray();

                if (recipients.Any(string.IsNullOrEmpty) || recipients.Any(r => !r.All(char.IsLetter)))
                {
                    RHMessageBoxHelper.ShowOKMessage(Resources.InvalidRecipientDesc, Resources.InvalidRecipient);
                    return [];
                }

                HashSet<string> uniqueRecipients = new(StringComparer.OrdinalIgnoreCase);
                foreach (var recipient in recipients)
                {
                    if (!uniqueRecipients.Add(recipient))
                    {
                        RHMessageBoxHelper.ShowOKMessage($"{Resources.DuplicateRecipientDesc}: {recipient}", Resources.DuplicateRecipient);
                        return [];
                    }
                }
                return recipients;
            }
        }

        /// <summary>
        /// Generates a confirmation message based on the provided parameters.
        /// </summary>
        /// <param name="sendToAllCharacters">Flag to send to all characters.</param>
        /// <param name="sendToAllOnline">Flag to send to all online characters.</param>
        /// <param name="sendToAllOffline">Flag to send to all offline characters.</param>
        /// <param name="recipients">Array of recipient names.</param>
        /// <returns>A confirmation message string.</returns>
        public static string GetConfirmationMessage(bool sendToAllCharacters, bool sendToAllOnline, bool sendToAllOffline, string[] recipients)
        {
            return sendToAllCharacters ? string.Format(Resources.SendMailMessageAll, recipients.Length) :
                   sendToAllOnline ? string.Format(Resources.SendMailMessageAllOnline, recipients.Length, Resources.Online) :
                   sendToAllOffline ? string.Format(Resources.SendMailMessageAllOnline, recipients.Length, Resources.Offline) :
                   $"{Resources.SendMailMessage}\n{string.Join(", ", recipients)}";
        }

        /// <summary>
        /// Generates a send message based on the provided parameters.
        /// </summary>
        /// <param name="sendToAllCharacters">Flag to send to all characters.</param>
        /// <param name="sendToAllOnline">Flag to send to all online characters.</param>
        /// <param name="sendToAllOffline">Flag to send to all offline characters.</param>
        /// <param name="successfulRecipients">List of successful recipients.</param>
        /// <param name="failedRecipients">List of failed recipients.</param>
        /// <returns>A send message string.</returns>
        public static string GetSendMessage(bool sendToAllCharacters, bool sendToAllOnline, bool sendToAllOffline, List<string> successfulRecipients, List<string> failedRecipients)
        {
            string baseMessage = sendToAllCharacters ? string.Format(Resources.SendMailMessageAllSuccess, successfulRecipients.Count) :
                                 sendToAllOnline ? string.Format(Resources.SendMailMessageAllOnlineSuccess, successfulRecipients.Count, Resources.Online) :
                                 sendToAllOffline ? string.Format(Resources.SendMailMessageAllOnlineSuccess, successfulRecipients.Count, Resources.Offline) :
                                 string.Format(Resources.SendMailMessageSuccess, successfulRecipients.Count);

            if (failedRecipients.Count > 0)
            {
                string failedRecipientsMessage = string.Format(Resources.FailedRecipientsMessage, failedRecipients.Count, "\n" + string.Join("\n", failedRecipients));
                baseMessage += "\n\n" + failedRecipientsMessage;
            }

            return baseMessage;
        }
    }
}