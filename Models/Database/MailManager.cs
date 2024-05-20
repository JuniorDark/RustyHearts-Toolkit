using RHToolkit.Models.MessageBox;
using RHToolkit.Properties;
using RHToolkit.Services;

namespace RHToolkit.Models.Database
{
    public class MailManager(IDatabaseService databaseService)
    {
        private readonly IDatabaseService _databaseService = databaseService;

        public async Task SendMailByGMAsync(string characterName, string? message)
        {
            if (message != null)
            {
                message = message.Replace("'", "''");
            }
            message += $"<br><br><right>{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            try
            {
                Guid senderId = Guid.Empty;

                await _databaseService.InsertMailAsync(senderId, senderId, "GM", characterName, message, 0, 7, 0, Guid.NewGuid(), 0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending mail: {ex.Message}", ex);
            }
        }

        public async Task<(List<string> successfulRecipients, List<string> failedRecipients)> SendMailAsync(
        string sender,
        string? message,
        int gold,
        int itemCharge,
        int returnDays,
        string[] recipients,
        List<ItemData> itemDataList)
        {
            if (message != null)
            {
                message = message.Replace("'", "''");
            }

            message += $"<br><br><right>{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            int createType = (itemCharge != 0) ? 5 : 0;
            List<string> successfulRecipients = [];
            List<string> failedRecipients = [];

            foreach (var currentRecipient in recipients)
            {
                Guid mailId = Guid.NewGuid();

                (Guid? senderCharacterId, Guid? senderAuthId, string? senderAccountName) = await _databaseService.GetCharacterInfoAsync(sender);
                (Guid? recipientCharacterId, Guid? recipientAuthId, string? recipientAccountName) = await _databaseService.GetCharacterInfoAsync(currentRecipient);

                if (senderCharacterId == null && createType == 5)
                {
                    string failedRecipientMessage = string.Format(Resources.InvalidSenderDesc, sender);
                    failedRecipients.Add(failedRecipientMessage);
                    continue;
                }

                if (senderCharacterId == null)
                {
                    senderCharacterId = Guid.Empty;
                    senderAuthId = Guid.Empty;
                }

                if (senderCharacterId == recipientCharacterId)
                {
                    string failedRecipientMessage = string.Format(Resources.SendMailSameName, sender);
                    failedRecipients.Add(failedRecipientMessage);
                    continue;
                }

                if (recipientCharacterId == null)
                {
                    string failedRecipientMessage = string.Format(Resources.NonExistentRecipient, currentRecipient);
                    failedRecipients.Add(failedRecipientMessage);
                    continue;
                }

                string auditMessage = "";

                if (gold > 0)
                {
                    auditMessage += $"[<font color=blue>{Resources.GMAuditAttachGold} - {gold}</font>]<br></font>";
                }

                if (itemDataList != null)
                {
                    foreach (ItemData itemData in itemDataList)
                    {
                        if (itemData.ID != 0)
                        {
                            auditMessage += $"[<font color=blue>{Resources.GMAuditAttachItem} - {itemData.ID} ({itemData.Amount})</font>]<br></font>";
                            await _databaseService.InsertMailItemAsync(itemData, recipientAuthId, recipientCharacterId, mailId, itemData.SlotIndex);
                        }
                    }
                }

                await _databaseService.InsertMailAsync(senderAuthId, senderCharacterId, sender, currentRecipient, message, gold, returnDays, itemCharge, mailId, createType);
                await _databaseService.GMAuditAsync(recipientAccountName!, recipientCharacterId, currentRecipient, Resources.SendMail, $"<font color=blue>{Resources.SendMail}</font>]<br><font color=red>{Resources.Sender}: RHToolkit: {senderCharacterId}, {Resources.Recipient}: {currentRecipient}, GUID:{{{recipientCharacterId}}}<br></font>" + auditMessage);

                successfulRecipients.Add(currentRecipient);
            }

            return (successfulRecipients, failedRecipients);
        }

        private static readonly char[] separator = [','];

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
                    RHMessageBox.ShowOKMessage(Resources.InvalidRecipientDesc, Resources.InvalidRecipient);
                    return [];
                }

                HashSet<string> uniqueRecipients = new(StringComparer.OrdinalIgnoreCase);
                foreach (var recipient in recipients)
                {
                    if (!uniqueRecipients.Add(recipient))
                    {
                        RHMessageBox.ShowOKMessage($"{Resources.DuplicateRecipientDesc}: {recipient}", Resources.DuplicateRecipient);
                        return [];
                    }
                }
                return recipients;
            }
        }

        public static string GetConfirmationMessage(bool sendToAllCharacters, bool sendToAllOnline, bool sendToAllOffline, string[] recipients)
        {
            return sendToAllCharacters ? string.Format(Resources.SendMailMessageAll, recipients.Length) :
                   sendToAllOnline ? string.Format(Resources.SendMailMessageAllOnline, recipients.Length, Resources.Online) :
                   sendToAllOffline ? string.Format(Resources.SendMailMessageAllOnline, recipients.Length, Resources.Offline) :
                   $"{Resources.SendMailMessage}\n{string.Join(", ", recipients)}";
        }

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
