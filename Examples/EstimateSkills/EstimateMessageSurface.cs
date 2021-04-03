using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using SlackSkills;
using SlackSkills.Surface;

namespace EstimateSkills
{
    public class EstimateMessageSurface : SlackSurface
    {
        public string Description { get; set; }
        public string Scale { get; set; }
        public string Risk { get; set; }

        private readonly SectionLayout _messageLayout = new("estimate-messages") { text = "\n" };
        private readonly ContextLayout _votedLayout   = new("estimate-voted-messages") { };
        private readonly ContextLayout _riskVotedLayout   = new("estimate-risk-voted") { };

        private readonly List<UserVote> _userVotes = new();

        public bool Revealed { get; set; }

        //new Option( "Agile 0,½,1,2,3,5,8,13,20,40,100,:shrug:,:question:",  "Agile" ),
        //new Option( "Fibonacci 0,1,2,3,5,8,13,21,34,55,:shrug:,:question:", "Fibonacci" ),
        //new Option( "Linear 0,1,2,3,4,5,6,7,8,9,:shrug:,:question:",        "Linear" ),
        //new Option( "t-shirt XS,S,M,L,XL,XXL,:shrug:,:question:",           "TShirt" ),
        //new Option( "Binary 0,1,2,4,8,16,32,64,128,:shrug:,:question:",     "Binary" )

        //new Option( "Ignore",                                     "Ignore" ),
        //new Option( "t-shirt XS,S,M,L,XL,XXL,:shrug:,:question:", "TShirt" ),

        private readonly List<string> _agile     = new() { "0", "½", "1", "2", "3", "5", "8", "13", "20", ":shrug:" };
        private readonly List<string> _fibonacci = new() { "0", "1", "2", "3", "5", "8", "13", "21", "34", ":shrug:" };
        private readonly List<string> _linear    = new() { "0", "1", "2", "3", "4", "5", "6", "7", "8", ":shrug:" };
        private readonly List<string> _shirt     = new() { "XS", "S", "M", "L", "XL", "XXL", ":shrug:" };
        private readonly List<string> _binary    = new() { "0", "1", "2", "4", "8", "16", "32", "64", "128", ":shrug:" };

        private readonly List<string> _risk      = new() { "XS", "S", "M", "L", "XL", "XXL", ":shrug:" };

        private bool IncludeRisk =>
            !string.IsNullOrEmpty( Risk );

        public EstimateMessageSurface(ISlackApp app) : base(app)
        {
        }

        public override List<Layout> BuildLayouts()
        {
            Title ??= "story";

            var fresh = !HasLayouts;

            if (Revealed)
                ClearLayouts();

            if (fresh || Revealed)
            {
                Add(new HeaderLayout($"Estimate: {Title}"));
                if (!string.IsNullOrEmpty(Description))
                    Add(new ContextLayout().Add(new MarkdownElement(Description)));
                Add(new DividerLayout());
            }

            if (fresh && !Revealed)
            {
                List<string> selectedScale;
                switch (Scale)
                {
                    case "Fibonacci":
                        selectedScale = _fibonacci;

                        break;
                    case "Linear":
                        selectedScale = _linear;

                        break;
                    case "TShirt":
                        selectedScale = _shirt;

                        break;
                    case "Binary":
                        selectedScale = _binary;

                        break;
                    case "Agile":
                        selectedScale = _agile;

                        break;
                    default:
                        selectedScale = _agile;

                        break;
                }
                var layout = new ActionsLayout();
                foreach (var item in selectedScale)
                {
                    if (layout.elements.Count >= 5)
                    {
                        Add(layout);
                        layout = new ActionsLayout();
                    }

                    layout.Add(new ButtonElement(item, item) { Clicked = OnVote, value = item });
                }
                Add(layout);

                if (IncludeRisk)
                {
                    Add( new ContextLayout().Add( new PlainTextElement( "Risk" )));
                    var riskLayout = new ActionsLayout();
                    foreach (var item in _risk)
                    {
                        if (riskLayout.elements.Count >= 5)
                        {
                            Add(riskLayout);
                            riskLayout = new ActionsLayout();
                        }

                        riskLayout.Add(new ButtonElement(item, item) { Clicked = OnRiskVote, value = item });
                    }
                    Add(riskLayout);
                }
            }
            UpdateVotes().GetAwaiter().GetResult();
            if (fresh || Revealed)
            {
                // Only show images
                if (Revealed)
                    Add(_messageLayout);
                else
                {
                    Add( _votedLayout );
                    if (IncludeRisk)
                        Add( _riskVotedLayout );
                }
            }

            return base.BuildLayouts();
        }

        private async Task UpdateVotes()
        {
            _votedLayout.elements.Clear();
            _riskVotedLayout.elements.Clear();

            foreach (var userVote in _userVotes)
            {
                var userContext = await SlackApp!.GetUserContext( userVote.User.id );
                var name        = userContext?.User.profile?.display_name_normalized ?? 
                                  userContext?.User.real_name ?? 
                                  userContext?.User.name ?? 
                                  userVote.User.id;
                if (userVote.Voted)
                {
                    _votedLayout.elements.Add(
                                              new ImageElement(
                                                               actionId: null, 
                                                               imageUrl: userContext?.User?.profile?.image_32 ?? "", 
                                                               altText: name));
                }
                if (userVote.RiskVoted)
                {
                    _riskVotedLayout.elements.Add(
                                              new ImageElement(actionId: null, 
                                                               imageUrl: userContext?.User.profile?.image_32 ?? "", 
                                                               altText: name));
                }
            }

            var voteCount = _userVotes.Count(vote => vote.Voted);
            _votedLayout.elements.Add(new PlainTextElement($"{voteCount} voted"));

            if (IncludeRisk)
            {
                var riskCount = _userVotes.Count( vote => vote.RiskVoted );
                _riskVotedLayout.elements.Add( new PlainTextElement( $"{riskCount} risk voted" ) );
                _messageLayout.text = new Markdown(
                    string.Join("\n", _userVotes)                               + "\n\n" +
                                      string.Join("\n", _userVotes.Select(v => v.RiskToString())) + "\n");
            }
            else
            {
                _messageLayout.text = new Markdown(string.Join("\n", _userVotes) + "\n");
            }
        }

        private void OnVote(SlackSurface surface, ButtonElement button, BlockActions actions)
        {
            var userVote = _userVotes.FirstOrDefault(uv => uv.User.id == actions.user.id);
            if (userVote == null)
            {
                userVote = new UserVote { User = actions.user };
                _userVotes.Add(userVote);
            }
            userVote.Vote = button.value;

            SlackApp!.Update(this);
        }

        private void OnRiskVote(SlackSurface surface, ButtonElement button, BlockActions actions)
        {
            var userVote = _userVotes.FirstOrDefault(uv => uv.User.id == actions.user.id);
            if (userVote == null)
            {
                userVote = new UserVote { User = actions.user };
                _userVotes.Add(userVote);
            }
            userVote.RiskVote  = button.value;

            SlackApp!.Update(this);
        }

        public void Reveal()
        {
            Revealed = true;
            foreach (var vote in _userVotes)
                vote.Revealed = true;

            SlackApp!.Update(this);
        }


        public class UserVote
        {
            public User User { get; set; }

            public string Name =>
                User.real_name ?? User.name ?? User.id;

            public bool Voted =>
                !string.IsNullOrEmpty( Vote );

            public bool RiskVoted =>
                !string.IsNullOrEmpty( RiskVote );

            public bool Revealed { get; set; }

            public string? Vote { get; set; }
            public string? RiskVote { get; set; }

            public override string ToString()
            {
                return Revealed ? $"*{Name}* voted *{Vote}*" :
                       Voted    ? $"*{Name}* has Voted"
                                : $"*{Name}* has joined";
            }

            public string RiskToString()
            {
                return Revealed  ? $"*{Name}* risk voted *{RiskVote}*" :
                       RiskVoted ? $"*{Name}* has Risk Voted"
                                 : $"*{Name}* has joined";
            }
        }
    }
}
