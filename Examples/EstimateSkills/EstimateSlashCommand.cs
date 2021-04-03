using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Asciis;

using SlackSkills;
using SlackSkills.Surface;

namespace EstimateSkills
{
    [Command("/estimate", Description = "Estimate story")]
    internal class EstimateSlashCommand : SlackSlashCommand
    {
        public override void OnCommand(List<string> args)
        {
            var dialog = new EstimateDialog( SlackApp!, Message!.channel_id, Message.user_id, Message!.text );

            SlackApp!.OpenModalSurface(dialog, Message!.trigger_id);
        }
    }
}
