using RHToolkit.Models.MessageBox;
using RHToolkit.Services;

namespace RHToolkit.Models.Database
{
    public class CharacterOnlineValidator(IDatabaseService databaseService)
    {
        private readonly IDatabaseService _databaseService = databaseService;

        public async Task<bool> IsCharacterOnlineAsync(string characterName)
        {
            if (await _databaseService.IsCharacterOnlineAsync(characterName))
            {
                RHMessageBox.ShowOKMessage($"The character '{characterName}' is currently online. You can't edit an online character.", "Info");
                return true;
            }

            return false;
        }
    }

}
