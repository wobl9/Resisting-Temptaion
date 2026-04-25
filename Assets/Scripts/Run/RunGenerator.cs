using System;
using System.Collections.Generic;

namespace ShatteredForge.Run
{
    public enum RoomType
    {
        Combat,
        Event,
        Forge,
        Shop,
        Elite,
        Rest,
        Boss
    }

    public class RunGenerator
    {
        private readonly Random _random;

        public RunGenerator(int seed)
        {
            _random = new Random(seed);
        }

        public List<RoomType> GenerateAct(int roomCount)
        {
            var rooms = new List<RoomType>(roomCount);
            for (var i = 0; i < roomCount - 1; i++)
            {
                rooms.Add(GetWeightedRoom());
            }

            rooms.Add(RoomType.Boss);
            return rooms;
        }

        private RoomType GetWeightedRoom()
        {
            var roll = _random.NextDouble();
            if (roll < 0.60) return RoomType.Combat;
            if (roll < 0.70) return RoomType.Event;
            if (roll < 0.78) return RoomType.Forge;
            if (roll < 0.86) return RoomType.Shop;
            if (roll < 0.94) return RoomType.Elite;
            return RoomType.Rest;
        }
    }
}
