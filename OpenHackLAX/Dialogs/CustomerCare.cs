using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace OpenHackLAX.Dialogs
{
    public class CustomerCare
    {
        //public Task StartAsync(IDialogContext context)
        //{
        //    context.Wait(MessageReceivedWelcome);
        //    return Task.CompletedTask;

        //}

        //private async Task MessageReceivedWelcome(IDialogContext context, IAwaitable<object> result)
        //{
        //    PromptDialog.Choice(context, MessageReceivedStart, new string[] { "FAQ", "State", "Alliance" }, "Hi, I can help you with these:", "Not a valid option", 3);
        //}

        //public async Task MessageReceivedStart(IDialogContext context, IAwaitable<string> argument)
        //{

        //    string message = await argument;

        //    if (message.Equals("FAQ", StringComparison.InvariantCultureIgnoreCase))

        //    {

        //        await context.PostAsync("You sent FAQ");

        //    }

        //    else if (message.Equals("State", StringComparison.InvariantCultureIgnoreCase))

        //    {

        //        await context.PostAsync("You sent State");

        //    }
        //    else if (message.Equals("Pay Tax", StringComparison.InvariantCultureIgnoreCase))

        //    {

        //        await context.PostAsync("You sent Alliance");

        //    }

        //}
    }
}
