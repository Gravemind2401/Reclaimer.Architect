using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Reclaimer.Resources;
using System.Xml;

namespace Reclaimer.Models.Ai
{
    public class AiSquadHierarchy
    {
        internal Dictionary<string, XmlNode> AiNodes { get; }
        public ObservableCollection<AiNamedBlock> SquadGroups { get; }
        public ObservableCollection<AiZone> Zones { get; }

        public AiSquadHierarchy(ScenarioModel scenario)
        {
            AiNodes = scenario.Sections[Section.Squads].Node.SelectNodes("./tagblock[@id]")
                .OfType<XmlNode>()
                .ToDictionary(n => n.Attributes["id"].Value);

            SquadGroups = new ObservableCollection<AiNamedBlock>();
            Zones = new ObservableCollection<AiZone>();
        }
    }

    public class AiZone : AiNamedBlock
    {
        public ObservableCollection<AiFiringPosition> FiringPositions { get; }
        public ObservableCollection<AiArea> Areas { get; }
        public ObservableCollection<AiSquad> Squads { get; }

        public AiZone(BlockReference blockRef, int index)
            : base(blockRef, index)
        {
            FiringPositions = new ObservableCollection<AiFiringPosition>();
            Areas = new ObservableCollection<AiArea>();
            Squads = new ObservableCollection<AiSquad>();
        }
    }

    public class AiSquad : AiNamedBlock
    {
        private int zoneIndex;
        public int ZoneIndex
        {
            get { return zoneIndex; }
            set { SetProperty(ref zoneIndex, value); }
        }

        public ObservableCollection<AiEncounter> Encounters { get; }

        public AiSquad(BlockReference blockRef, int index)
            : base(blockRef, index)
        {
            Encounters = new ObservableCollection<AiEncounter>();
        }
    }

    public class AiEncounter : AiNamedBlock
    {
        public ObservableCollection<AiStartingLocation> StartingLocations { get; }

        public AiEncounter(BlockReference blockRef, int index)
            : base(blockRef, index)
        {
            StartingLocations = new ObservableCollection<AiStartingLocation>();
        }
    }
}
