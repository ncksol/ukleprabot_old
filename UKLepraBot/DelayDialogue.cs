using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using UKLepraBot.Properties;

namespace UKLepraBot
{
    [Serializable]
    public class DelayDialogue:IDialog
    {
        public Int64 ChatId { get; set; }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(Resume);
        }

        private async Task Resume(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var activity = (Activity) await result;

            var message = activity.RemoveRecipientMention().ToLower().TrimEnd('@');
            if (message == "/delay")
            {
                Tuple<int,int> currentChatDelay;
                Settings.Default.Delay.TryGetValue(ChatId, out currentChatDelay);
                if (currentChatDelay == null || (currentChatDelay.Item1 == 0 && currentChatDelay.Item2 == 0))
                {
                    var replyMessage = "Currently, I have no delay settings set. You can set my delay settings by sending them to me now in the format x - y. Where x and y are integers and y > x.";
                    PromptDialog.Text(context, DelayPrompt, replyMessage);
                }
                else
                {
                    PromptDialog.Text(context, DelayPrompt, $"Currently, I am randomly skipping between {currentChatDelay.Item1} and {currentChatDelay.Item2} messages. You can change my delay settings by sending them to me now in the format x - y. Where x and y are integers and y > x.");
                }
            }            
            else
            {
                await context.PostAsync("Нифига не поняла! Давай сначала.");
                context.Done("");
            }
        }

        private async Task DelayPrompt(IDialogContext context, IAwaitable<string> result)
        {
            var delayRegex = new Regex(@"\s*(?'min'\d+)\s*-\s*(?'max'\d+)");
            var message = await result;

            if (delayRegex.IsMatch(message))
            {
                var match = delayRegex.Match(message);
                var delayMinValue = Convert.ToInt32(match.Groups["min"].Value);
                var delayMaxValue = Convert.ToInt32(match.Groups["max"].Value);
                if (delayMinValue >= delayMaxValue)
                {
                    var user = "";//activity.From?.Name;
                    var replyMessage = (!string.IsNullOrEmpty(user) ? $"@{user} " : "") + "Ты что гуманитарий? Я же сказала y больше чем x.";
                    await context.PostAsync(replyMessage);
                    context.Wait(Resume);                    
                    return;
                }

                var delay = new Tuple<int, int>(delayMinValue, delayMaxValue);
                if (!Settings.Default.Delay.ContainsKey(ChatId))
                    Settings.Default.Delay.Add(ChatId, delay);
                else
                    Settings.Default.Delay[ChatId] = delay;

                await
                    context.PostAsync(
                        $"Delay settings successfuly set. I will now randomly skip between {delayMinValue} and {delayMaxValue} messages.");

                context.Done("");
            }
        }
    }
}