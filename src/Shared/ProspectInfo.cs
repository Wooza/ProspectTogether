using Newtonsoft.Json;
using ProtoBuf;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Config;

namespace ProspectTogether.Shared
{
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class ProspectInfo
    {
        private static readonly Dictionary<RelativeDensity, string> RelativeDensityToLang = new()
        {
                { RelativeDensity.VeryPoor, "propick-density-verypoor" },
                { RelativeDensity.Poor, "propick-density-poor"},
                { RelativeDensity.Decent, "propick-density-decent" },
                { RelativeDensity.High , "propick-density-high" },
                { RelativeDensity.VeryHigh , "propick-density-veryhigh" },
                { RelativeDensity.UltraHigh , "propick-density-ultrahigh" }
            };

        [ProtoMember(1)]
        public readonly ChunkCoordinate Chunk;

        /// <summary>
        /// A sorted list of all ore occurencies in this chunk. The ore with the highest relative density is first.
        /// </summary>
        [ProtoMember(2)]
        public List<OreOccurence> Values = new();

        /// <summary>
        /// The return value from <see cref="GetMessage"/> if it was called at least once. Used to avoid multiple StringBuilder calls.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        private string _MessageCache;

        public ProspectInfo()
        { }

        [JsonConstructor]
        public ProspectInfo(ChunkCoordinate chunk, List<OreOccurence> values)
        {
            Chunk = chunk;
            Values = values;
        }

        public bool Equals(ProspectInfo other)
        {
            return Chunk.X == other.Chunk.X && Chunk.Z == other.Chunk.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is ProspectInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Chunk.X * 397 ^ Chunk.Z;
            }
        }

        public string GetMessage()
        {
            if (_MessageCache == null)
            {

                StringBuilder sb = new();
                // Restore sort order
                Values = Values.OrderBy(v => v.RelativeDensity).ThenBy(v => v.AbsoluteDensity).Reverse().ToList();

                if (Values.Count > 0)
                {
                    sb.AppendLine(Lang.Get("propick-reading-title", Values.Count));
                    List<string> traces = new();

                    foreach (var elem in Values)
                    {
                        if (elem.RelativeDensity > RelativeDensity.Miniscule)
                        {
                            sb.AppendLine(Lang.Get("propick-reading", Lang.Get(RelativeDensityToLang[elem.RelativeDensity]), elem.PageCode, Lang.Get(elem.Name), elem.AbsoluteDensity.ToString("0.##")));
                        }
                        else
                        {
                            traces.Add(Lang.Get(elem.Name));
                        }
                    }
                    if (traces.Count > 0)
                    {
                        sb.Append(Lang.Get("Miniscule amounts of {0}", string.Join(", ", traces)));
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.Append(Lang.Get("propick-noreading"));
                }

                _MessageCache = sb.ToString();
            }
            return _MessageCache;
        }

        public RelativeDensity GetValueOfOre(string oreName)
        {
            foreach (var ore in Values)
            {
                if (Lang.Get(ore.Name).ToLower() == oreName.ToLower() || ore.Name.ToLower() == oreName.ToLower())
                    return ore.RelativeDensity;
            }
            return RelativeDensity.Zero;
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public readonly struct OreOccurence
    {
        [ProtoMember(1)]
        public readonly string Name;
        [ProtoMember(2)]
        public readonly string PageCode;
        [ProtoMember(3, IsRequired = true)]
        public readonly RelativeDensity RelativeDensity;
        [ProtoMember(4)]
        public readonly double AbsoluteDensity;

        [JsonConstructor]
        public OreOccurence(string name, string pageCode, RelativeDensity relativeDensity, double absoluteDensity)
        {
            Name = name;
            PageCode = pageCode;
            RelativeDensity = relativeDensity;
            AbsoluteDensity = absoluteDensity;
        }
    }

    public enum RelativeDensity
    {
        Zero,
        Miniscule,
        VeryPoor,
        Poor,
        Decent,
        High,
        VeryHigh,
        UltraHigh
    }

    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public struct ChunkCoordinate
    {
        [ProtoMember(1)]
        public int X;
        [ProtoMember(2)]
        public int Z;

        public ChunkCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }
    }
}
