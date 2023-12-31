using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FruitBowlBot_v2.Commands
{
    public interface IPluginCommand
    {
        string PluginName { get; }
        string Command { get; }
        IEnumerable<string> Aliases { get; }
        bool Loaded { get; set; }

        bool ModOnly { get; set; }



        IEnumerable<string> Help { get; }
        Task<string> Action(Message message);
    }

    public class Message
    {
        public string RawMessage { get; set; }
        public string Channel { get; set; }
        public string Command { get; set; }
        public string Username { get; set; }
        public bool IsModerator { get; set; }
        public List<string> Arguments { get; set; }
    }
}
