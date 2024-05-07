namespace BigOne.Modules.GeneralBotModules;

using System;
using System.Threading.Tasks;
using Discord.Interactions;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System.Configuration;

/// <summary>
///     Presents some of the main features of the Lavalink4NET-Library.
/// </summary>
[RequireContext(ContextType.Guild)]
public sealed class ChatModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ConversationService _conversationService;

    public ChatModule(ConversationService conversationService)
    {
        ArgumentNullException.ThrowIfNull(conversationService);
        _conversationService = conversationService;
    }

    [SlashCommand("prompt", description: "Generate AI Response", runMode: RunMode.Async)]
    public async Task Prompt(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        // API Key should be securely stored and retrieved, not hardcoded
        var apiKey = ConfigurationManager.AppSettings["OpenAIAPIKey"];
        var api = new OpenAIAPI(apiKey);

        try
        {
            var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
            {
                Model = Model.ChatGPTTurbo,
                Temperature = 0.1,
                MaxTokens = 50,
                Messages = new ChatMessage[] {
                    new ChatMessage(ChatMessageRole.User, query)
                }
            });

            var responseText = result.ToString();

            if (!string.IsNullOrEmpty(responseText))
            {
                await FollowupAsync($"**Prompt:** {query}\n\n**Answer:** {responseText}").ConfigureAwait(false);
            }
            else
            {
                await FollowupAsync("Failed to get a response from OpenAI.").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await FollowupAsync("Error while fetching data from OpenAI: " + ex.Message).ConfigureAwait(false);
        }
    }


    [SlashCommand("chat", description: "Create or continue conversation", runMode: RunMode.Async)]
    public async Task Chat(string query)
    {
        try
        {
            await DeferAsync().ConfigureAwait(false);

            var apiKey = ConfigurationManager.AppSettings["OpenAIAPIKey"];
            var api = new OpenAIAPI(apiKey);

            var userId = Context.User.Id;
            if (!_conversationService.UserConversations.ContainsKey(userId))
            {
                var chat = api.Chat.CreateConversation();
                chat.Model = Model.GPT4_Turbo;
                chat.RequestParameters.Temperature = 0;
                _conversationService.UserConversations.Add(userId, chat);
                chat.AppendUserInput(query);
                var response = await chat.GetResponseFromChatbotAsync();
                var responseText = response.ToString();

                if (!string.IsNullOrEmpty(responseText))
                {
                    await FollowupAsync($"**Prompt:** {query}\n\n**Answer:** {responseText}").ConfigureAwait(false);
                }
                else
                {
                    await FollowupAsync("Failed to get a response from OpenAI.").ConfigureAwait(false);
                }
                _conversationService.UserConversations[userId] = chat;
            }
            else
            {
                var chat = _conversationService.UserConversations[userId];
                chat.AppendUserInput(query);
                var response = await chat.GetResponseFromChatbotAsync();
                var responseText = response.ToString();
                if (!string.IsNullOrEmpty(responseText))
                {
                    await FollowupAsync($"**Prompt:** {query}\n\n**Answer:** {responseText}").ConfigureAwait(false);
                }
                else
                {
                    await FollowupAsync("Failed to get a response from OpenAI.").ConfigureAwait(false);
                }
                _conversationService.UserConversations[userId] = chat;
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await FollowupAsync("Something went wrong managing conversations").ConfigureAwait(false);
        }
    }
}

public class ConversationService
{
    public Dictionary<ulong, Conversation> UserConversations { get; } = new Dictionary<ulong, Conversation>();
}