using System;
using Modding;

namespace CommunityHighlander.Framework
{
    public class EventListenerTemplate
    {
        private const string constructorError = "Event listeners cannot be instantiated without a mod record!";
        private const string getVersionError = "Version getters should only be called in classes that inherit from the template!";
        private const string eventMethodError = "Event methods should only be called in classes that inherit from the template!";

        protected ModRecord modRecord;

        public EventListenerTemplate()
        {
            throw new InvalidOperationException(constructorError);
        }

        public EventListenerTemplate(ModRecord modRecord)
        {
            this.modRecord = modRecord;
        }

        public virtual string HighlanderVersionMinimum
        {
            get
            {
                throw new NotImplementedException(getVersionError);
            }
        }

        public virtual string HighlanderVersionMaximum
        {
            get
            {
                throw new NotImplementedException(getVersionError);
            }
        }

        public virtual void OnModLoadedAtStartup()
        {
            throw new NotImplementedException(eventMethodError);
        }

        public virtual void OnModLoadedInLobby()
        {
            throw new NotImplementedException(eventMethodError);
        }
    }
}
