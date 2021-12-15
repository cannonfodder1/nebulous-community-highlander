using System;
using Modding;

namespace CommunityHighlander
{
    public class CH_EventHookTemplate
    {
        private const string constructorError = "Event hooks cannot be instantiated without a mod record!";
        private const string getVersionError = "Version getters should only be called in classes that inherit from the template!";
        private const string eventMethodError = "Event methods should only be called in classes that inherit from the template!";

        protected ModRecord modRecord;

        public CH_EventHookTemplate()
        {
            throw new InvalidOperationException(constructorError);
        }

        public CH_EventHookTemplate(ModRecord modRecord)
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
