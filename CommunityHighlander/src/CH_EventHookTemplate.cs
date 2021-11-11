using System;
using Modding;

namespace CommunityHighlander
{
    public class CH_EventHookTemplate
    {
        protected ModRecord modRecord;

        public CH_EventHookTemplate()
        {
            throw new NotImplementedException("CH_EventHookTemplate should never be instantiated without a mod record!");
        }

        public CH_EventHookTemplate(ModRecord modRecord)
        {
            this.modRecord = modRecord;
        }

        public virtual void OnModLoadedAtStartup()
        {
            throw new NotImplementedException("OnModLoadedAtStartup event hook should only be called in classes that inherit from the template!");
        }

        public virtual void OnModLoadedInLobby()
        {
            throw new NotImplementedException("OnModLoadedInLobby event hook should only be called in classes that inherit from the template!");
        }
    }
}
