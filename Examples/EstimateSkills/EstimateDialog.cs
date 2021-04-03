using System;
using System.Collections.Generic;

using SlackSkills;
using SlackSkills.Surface;

namespace EstimateSkills
{
    public class EstimateDialog : SlackSurface
    {
        public static List< Option > ScaleOptions = new List< Option >
                    {
                        new Option( "Agile 0,½,1,2,3,5,8,13,20,:shrug:",      "Agile" ),
                        new Option( "Fibonacci 0,1,2,3,5,8,13,21,34,:shrug:", "Fibonacci" ),
                        new Option( "Linear 0,1,2,3,4,5,6,7,8,:shrug:",       "Linear" ),
                        new Option( "t-shirt XS,S,M,L,XL,XXL,:shrug:",        "TShirt" ),
                        new Option( "Binary 0,1,2,4,8,16,32,64,:shrug:",      "Binary" )
                    };

        public static List< Option > RiskOptions = new List< Option >
                   {
                       new Option( "Ignore", "Ignore" ), 
                       new Option( "t-shirt XS,S,M,L,XL,XXL,:shrug:", "TShirt" ),
                   };

        private readonly string                 _channelId;
        private readonly string                 _userId;
        private          EstimateMessageSurface _estimateMessageSurface;
        private          SlackSurface           _revealSurface;

        public TextInputElement TitleInput { get; }
        private readonly TextInputElement _descriptionInput;
        public SelectElement ScaleSelect { get; }
        public SelectElement RiskSelect { get; }

        public string? EstimateTitle =>
            TitleInput?.value;

        public string? EstimateDescription =>
            _descriptionInput?.value;

        public EstimateDialog(
            ISlackApp app, 
            string channelId,
            string userId,
            string? title = null, 
            string? description = null) : base(app)
        {
            _channelId = channelId;
            _userId    = userId;
            TitleInput = new TextInputElement("estimate-title")
            {
                placeholder = "enter the title of the story",
                initial_value = title,
                max_length = 30
            };
            ScaleSelect = new SelectElement("estimate-scale-id", "Select scale")
            {
                action_id = "estimate-select-scale",
                options = ScaleOptions,
                initial_option = ScaleOptions[0]
            };
            RiskSelect = new SelectElement("estimate-risk-id", "Select risk")
            {
                action_id = "estimate-select-scale",
                options = RiskOptions,
                initial_option = RiskOptions[0]
            };

            _descriptionInput = new TextInputElement("estimate-description") { multiline = true, initial_value = description };

            Submitted = OnSubmitted;
        }

        public override List<Layout> BuildLayouts()
        {
            Title = "Estimate Story";

            ClearLayouts();
            Add(new InputLayout("Title", TitleInput, "estimate-title"));
            Add(new InputLayout("Description", _descriptionInput, "estimate-description") { optional = true });
            Add(new InputLayout("Scale", ScaleSelect, "estimate-scale"));
            Add(new InputLayout("Risk", RiskSelect, "estimate-risk"));

            return base.BuildLayouts();
        }

        private void OnSubmitted(ViewSubmission msg)
        {
            _estimateMessageSurface = new EstimateMessageSurface(SlackApp!)
                                      {
                                          Title       = EstimateTitle!,
                                          Description = EstimateDescription,
                                          Scale       = ScaleSelect.selected_option?.value,
                                          Risk        = RiskSelect.selected_option?.value
                                      };
            SlackApp!.OpenMessageSurface(_estimateMessageSurface, _channelId);

            // Send a message surface that is visible only to the Estimator
            _revealSurface = new SlackSurface(SlackApp) { Title = "Reveal" }
               .Add(new SectionLayout("estimate-reveal")
                    {
                        text = "Reveal all estimations to the team",
                        accessory = new ButtonElement("estimate-reveal-button", "Reveal")
                                    {
                                        Clicked = OnReveal,
                                        value   = "reveal"
                                    }
                    });
            SlackApp!.OpenMessageSurface(_revealSurface, _channelId, _userId);

        }

        private void OnReveal(SlackSurface surface, ButtonElement button, BlockActions actions)
        {
            // Remove the Reveal messages
            // For ephemeral messages, they can only be modified using the response_url
            var response = new SlashCommandResponse
                           {
                               response_type    = "ephemeral", // visible to only the user
                               replace_original = true,
                               delete_original  = true,
                               text             = ""
                           };
            SlackApp!.Respond(actions.response_url, response);

            // Change the estimate surface to reveal all the results
            _estimateMessageSurface.Reveal();
        }

    }
}
