First stable public
RELEASE v1.05 1/11/2016
http://steamcommunity.com/sharedfiles/filedetails/?id=572010326

Levitator Mod is intended as a robust and versatile mod development framework for Space Engineers (Keen Software House). Initially, it comes with one
module called NHBC, which stands for No Hale Bopp Cult. It prevents players from exploiting sucide as a mode of transportation. When spawning as an
astronaut, users will only spawn at their designated "home" Medical Room. A player may change their home Medical Room by traveling to a friendly,
active Medical Room and typing "/home" in chat.


DEVELOPMENT GUIDE
To extend the mod, derive from ModModule and edit LMClient, LMLocal, or LMServer (each of which is derived from ModComponent) so as to call RegisterModule()
on your module. That's it. It's that simple. If your module needs to process during the game's periodic update heartbeat, then have your ModModule call
RegisterForUpdate(). Each of the ModComponent subclasses does the exact same thing, but it processes messages from a different source. LMClient listens
for server messages, LMServer listens for client messages, and LMLocal listens for chat messages typed by the local player. Attach your ModModule to
the appropriate ModComponent and implement GetCommands() in your ModModule in order to register the command names which you wish to be dispatched to your module.

Look at the messages defined in Modding/Modules to see how to define new network messages and use the custom serializer. If you really want to, you can always
extend Network.cs to handle XML serialization, but XML has tons of tags and metadata and the game engine is limited to 4kB packets.

Levitator
