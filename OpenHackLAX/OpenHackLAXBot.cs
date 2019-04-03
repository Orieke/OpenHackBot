// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenHackLAX.Dialogs;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace OpenHackLAX
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class OpenHackLAXBot : IBot
    {
        private readonly OpenHackLAXAccessors _accessors;
        private readonly ILogger _logger;

        private const string WelcomeMessage = @"Hey there! I'm the ASH Music Festival Bot. I'm here to guide you around the fesival!";

        private const string InfoMessage = @"How would you like to explore the event?";


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="conversationState">The managed conversation state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public OpenHackLAXBot(ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            if (conversationState == null)
            {
                throw new System.ArgumentNullException(nameof(conversationState));
            }

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _accessors = new OpenHackLAXAccessors(conversationState)
            {
                CounterState = conversationState.CreateProperty<CounterState>(OpenHackLAXAccessors.CounterStateName),
            };

            _logger = loggerFactory.CreateLogger<OpenHackLAXBot>();
            _logger.LogTrace("Turn start.");
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Get the conversation state from the turn context.
                var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                // Bump the turn count for this conversation.
                state.TurnCount++;

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, state);

                // Save the new turn count into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);

                // Echo back to the user whatever they typed.
                var check = turnContext.Activity.Text;
                if (check.ToLower().Trim() == "faq" || check.ToLower().Trim() == "band search" || check.ToLower().Trim() == "navigate")
                {
                    var responseMessage = $"Turn {state.TurnCount}: You sent '{turnContext.Activity.Text}'\n";
                    await turnContext.SendActivityAsync(responseMessage);
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                }
                else
                {
                    await turnContext.SendActivityAsync("Not a valid option");
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                }
                
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded != null)
                {
                    // Iterate over all new members added to the conversation
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message
                        // the 'bot' is the recipient for events from the channel,
                        // turnContext.Activity.MembersAdded == turnContext.Activity.Recipient.Id indicates the
                        // bot was added to the conversation.
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync($"Hi there - {member.Name}. {WelcomeMessage}", cancellationToken: cancellationToken);
                            await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);

                            var welcomeCard = CreateAdaptiveCardAttachment();
                            var response = CreateResponse(turnContext.Activity, welcomeCard);
                            await turnContext.SendActivityAsync(response);

                            await SendSuggestedActionsAsync(turnContext, cancellationToken);
                            //await turnContext.Activity.(turnContext.Activity, () => new CustomerCare());                            

                            //await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }


        }
        private Activity CreateResponse(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        private Attachment CreateAdaptiveCardAttachment()
        {
            string[] paths = { ".", "Dialogs", "Welcome", "Resources", "weatherForecast.json" };
            string fullPath = Path.Combine(paths);
            var adaptiveCard = File.ReadAllText(fullPath);
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }

        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("I can help you with these?");
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "FAQ", Type = ActionTypes.ImBack, Value = "FAQ" },
                    new CardAction() { Title = "Band Search", Type = ActionTypes.ImBack, Value = "Band Search" },
                    new CardAction() { Title = "Navigate", Type = ActionTypes.ImBack, Value = "Navigate" },
                },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
