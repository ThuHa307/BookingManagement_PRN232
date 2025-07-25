using Microsoft.AspNetCore.SignalR;
using BusinessObjects.Domains;
using Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using BusinessObjects.Consts;

namespace WebAPI
{
    //[Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IAccountService _accountService;
        public ChatHub(IMessageService messageService, IAccountService accountService)
        {
            _messageService = messageService;
            _accountService = accountService;
        }
        public async Task SendMessage(int conversationId, int receiverId, string message, string imageUrl)
        {
            var senderIdStr = Context.User?.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (!int.TryParse(senderIdStr, out int senderId))
                return;

            var senderAccount = await _accountService.GetAccountByIdAsync(senderId);
            if (senderAccount != null)
            {
                senderAccount.LastActiveAt = System.DateTime.Now;
                await _accountService.UpdateAccountAsync(senderId, senderAccount);
            }

            var mess = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = message,
                ImageUrl = imageUrl,
                SentAt = System.DateTime.Now
            };
            await _messageService.AddMessageAsync(mess);

            await Clients.User(receiverId.ToString())
                .SendAsync("ReceiveMessage", senderId, message, imageUrl, conversationId);
        }

        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var account = await _accountService.GetAccountByIdAsync(userId);
                if (account != null)
                {
                    account.IsOnline = true;
                    await _accountService.UpdateAccountAsync(userId, account);
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var account = await _accountService.GetAccountByIdAsync(userId);
                if (account != null)
                {
                    account.IsOnline = false;
                    await _accountService.UpdateAccountAsync(userId, account);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
} 