using UnityEngine;

namespace Pif.AgentsAI
{
    public class PifAgentProfile : MonoBehaviour
    {
        [SerializeField] private string agentName = "AI";
        [SerializeField] private int reactionMs = 650;

        public string AgentName => agentName;
        public int ReactionMs => reactionMs;
    }
}
