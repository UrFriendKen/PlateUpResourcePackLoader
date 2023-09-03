using System.Collections.Generic;
using System.Reflection;
using XNode;

namespace KitchenResourcePackLoader.Utils
{
    public static class LogUtils
    {
        public static void LogGraph<T>(T graph) where T : NodeGraph, new()
        {
            if (graph == null)
                return;

            Main.LogInfo($"{graph.name}");

            FieldInfo f_ports = typeof(Node).GetField("ports", BindingFlags.NonPublic | BindingFlags.Instance);
            List<Node> nodes = graph?.nodes;

            if (nodes == null || nodes.Count == 0)
            {
                Main.LogWarning("No Nodes!");
            }    

            Main.LogInfo("Nodes:");
            for (int i = 0; i < nodes.Count; i++)
            {
                Main.LogInfo($"\t{i}:\t{(nodes[i]?.name ?? "null")} ({(nodes[i]?.GetType().ToString() ?? string.Empty)})");
            }

            Main.LogInfo("Connections:");
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                {
                    continue;
                }

                Main.LogInfo($"\t{i}:\t{nodes[i].name} ({(nodes[i].GetType().ToString() ?? string.Empty)})");

                object obj = f_ports.GetValue(nodes[i]);
                if (obj == null || !(obj is Dictionary<string, NodePort> nodeDictionary))
                {
                    Main.LogError($"\tFailed to get Node Dictionary from {nodes[i].GetType()}");
                    continue;
                }

                foreach (KeyValuePair<string, NodePort> nodePortItem in nodeDictionary)
                {
                    if (nodePortItem.Value == null)
                        continue;

                    NodePort nodePort = nodePortItem.Value;

                    Main.LogInfo($"\t\t\t{nodePortItem.Key} ({nodePortItem.Value.connectionType} {nodePortItem.Value.direction})");

                    if (nodePort.ConnectionCount <= 0)
                    {
                        Main.LogInfo("\t\t\t\tUnused");
                        continue;
                    }

                    List<int> connectedNodeIndices = new List<int>();
                    foreach (NodePort connectedPort in nodePort.GetConnections())
                    {
                        bool found = false;
                        for (int j = 0; j < nodes.Count; j++)
                        {
                            if (nodes[j] == connectedPort.node)
                            {
                                found = true;
                                connectedNodeIndices.Add(j);
                                break;
                            }
                        }
                        if (!found)
                        {
                            Main.LogWarning("\t\t\t\tFailed to find a connected node reference");
                        }
                    }
                    Main.LogInfo($"\t\t\t\t{string.Join(", ", connectedNodeIndices)}");
                }
            }
        }
    }
}
