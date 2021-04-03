using System;
using System.Collections.Generic;

using Asciis;

using SlackSkills;

namespace EstimateSkills
{
    [Command( "estimate_message", Description = "Estimate Message Shortcut")]
    public class EstimateMessageShortcutCommand : SlackMessageShortcutCommand
    {
        public override void OnCommand(List<string> args)
        {
            var dialog = new EstimateDialog(SlackApp!, Shortcut!.channel?.id, Shortcut.user?.id, description: Shortcut.message?.text);

            SlackApp!.OpenModalSurface(dialog, Shortcut?.trigger_id ?? "");
        }
    }
}
