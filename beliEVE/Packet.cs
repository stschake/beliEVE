using System;
using System.Collections.Generic;
using System.Linq;

namespace beliEVE
{
    public enum Direction
    {
        Incoming,
        Outgoing
    }

    public class Packet
    {
        public DateTime Time;
        public Direction Direction;
        public byte[] Data;
        public List<ExtraData> Extra = new List<ExtraData>();

        public bool HasExtra(ExtraDataType type)
        {
            return Extra.Any(e => e.Type == type);
        }

        public object GetExtra(ExtraDataType type)
        {
            if (!HasExtra(type))
                return null;
            return Extra.Where(e => e.Type == ExtraDataType.Traceback).First().Data;
        }

        public Packet(Direction dir, byte[] data)
        {
            Time = DateTime.Now;
            Direction = dir;
            Data = data;
        }
    }
}