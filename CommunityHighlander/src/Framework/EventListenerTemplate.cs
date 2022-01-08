using System;
using Modding;

namespace CommunityHighlander.Framework
{
    public abstract class EventListenerTemplate
    {
        private const string constructorError = "Event listeners cannot be instantiated without a mod record!";
        private const string eventHookError = "EventListenerTemplate's base event hook methods should not be called!";

        protected ModRecord modRecord;

        public EventListenerTemplate()
        {
            throw new InvalidOperationException(constructorError);
        }

        public EventListenerTemplate(ModRecord modRecord)
        {
            this.modRecord = modRecord;
        }

        public abstract string HighlanderVersionMinimum();

        public abstract string HighlanderVersionMaximum();

        public virtual void OnModLoadedAtStartup()
        {
            throw new NotImplementedException(eventHookError);
        }

        public virtual void OnModLoadedInLobby()
        {
            throw new NotImplementedException(eventHookError);
        }
    }
}
