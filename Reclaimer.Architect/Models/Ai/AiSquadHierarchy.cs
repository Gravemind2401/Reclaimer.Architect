﻿using System;
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
        public AiZone DefaultZone { get; }

        public AiSquadHierarchy(ScenarioModel scenario)
        {
            if (!scenario.Sections.ContainsKey(Section.Squads))
                AiNodes = new Dictionary<string, XmlNode>();
            else
            {
                AiNodes = scenario.Sections[Section.Squads].Node.SelectNodes("./tagblock[@id]")
                    .OfType<XmlNode>()
                    .ToDictionary(n => n.Attributes["id"].Value);
            }

            SquadGroups = new ObservableCollection<AiNamedBlock>();
            Zones = new ObservableCollection<AiZone>();
            DefaultZone = new AiZone(null, -1) { Name = "<none>" };
        }

        public IEnumerable<AiZone> EnumerateZones() => Enumerable.Repeat(DefaultZone, 1).Concat(Zones);
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
        public ObservableCollection<AiStartingLocation> GroupStartLocations { get; }
        public ObservableCollection<AiStartingLocation> SoloStartLocations { get; }

        public AiSquad(BlockReference blockRef, int index)
            : base(blockRef, index)
        {
            Encounters = new ObservableCollection<AiEncounter>();
            GroupStartLocations = new ObservableCollection<AiStartingLocation>();
            SoloStartLocations = new ObservableCollection<AiStartingLocation>();
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
