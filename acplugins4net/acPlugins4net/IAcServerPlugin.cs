using acPlugins4net.messages;

namespace acPlugins4net
{
    public interface IAcServerPlugin
    {
        string PluginName { get; }

        void OnInit(AcServerPluginManager manager);
        void OnConnected();
        void OnDisconnected();

        /// <summary>
        /// Called when a console command was entered.
        /// </summary>
        /// <param name="cmd">The console command.</param>
        /// <returns>Whether the command should be send to the next plugin.</returns>
        bool OnConsoleCommand(string cmd);

        void OnNewSession(MsgNewSession msg);
        void OnSessionEnded(MsgSessionEnded msg);
        void OnConnectionClosed(MsgConnectionClosed msg);
        void OnNewConnection(MsgNewConnection msg);
        void OnCarInfo(MsgCarInfo msg);
        void OnCarUpdate(MsgCarUpdate msg);
        void OnLapCompleted(MsgLapCompleted msg);
        void OnCollision(MsgClientEvent msg);
    }
}
