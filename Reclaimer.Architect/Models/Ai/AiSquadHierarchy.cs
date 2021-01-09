using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Reclaimer.Resources;

namespace Reclaimer.Models.Ai
{
    public class AiSquadHierarchy
    {
        public ObservableCollection<NamedBlock> SquadGroups { get; }
        public ObservableCollection<AiZone> Zones { get; }

        public AiSquadHierarchy()
        {
            SquadGroups = new ObservableCollection<NamedBlock>();
            Zones = new ObservableCollection<AiZone>();
        }
    }

    public class AiZone : NamedBlock
    {
        public ObservableCollection<AiFiringPosition> FiringPositions { get; }
        public ObservableCollection<NamedBlock> Areas { get; }
        public ObservableCollection<AiEncounter> Encounters { get; }

        public AiZone()
        {
            FiringPositions = new ObservableCollection<AiFiringPosition>();
            Areas = new ObservableCollection<NamedBlock>();
            Encounters = new ObservableCollection<AiEncounter>();
        }
    }

    public class AiEncounter : NamedBlock
    {
        private int zoneIndex;
        public int ZoneIndex
        {
            get { return zoneIndex; }
            set { SetProperty(ref zoneIndex, value); }
        }

        public ObservableCollection<AiSquad> Squads { get; }

        public AiEncounter()
        {
            Squads = new ObservableCollection<AiSquad>();
        }
    }

    public class AiSquad : NamedBlock
    {
        public ObservableCollection<AiStartingLocation> StartingLocations { get; }

        public AiSquad()
        {
            StartingLocations = new ObservableCollection<AiStartingLocation>();
        }
    }
}
