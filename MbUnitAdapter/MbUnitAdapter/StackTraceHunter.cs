using System;
using Gallio.Common.Markup;
using Gallio.Common.Markup.Tags;

namespace TestPlatform.Gallio
{
    public class StackTraceHunter : BaseTagVisitor
    {
        private readonly Action<string> action;

        public StackTraceHunter(Action<string> action)
        {
            this.action = action;
        }

        public override void VisitMarkerTag(MarkerTag tag)
        {
            if (tag.Class == Marker.StackTraceClass)
                action(tag.ToString());
            else
                base.VisitMarkerTag(tag);
        }
    }
}