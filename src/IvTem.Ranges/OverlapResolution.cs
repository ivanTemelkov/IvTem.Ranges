namespace IvTem.Ranges;

public enum OverlapResolution
{
    KeepLeftChunk,  // Keep the left range as-is; trim the head of the right to start at left.To
    KeepRightChunk, // Trim the tail of the left to end at right.From; keep the right as-is
}