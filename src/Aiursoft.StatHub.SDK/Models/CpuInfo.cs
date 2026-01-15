namespace Aiursoft.StatHub.SDK.Models;

public class CpuInfo(int usr, int sys, int idl, int wai, int stl)
{
    public int Usr { get; set; } = usr;
    public int Sys { get; set; } = sys;
    public int Idl { get; set; } = idl;
    public int Wai { get; set; } = wai;
    public int Stl { get; set; } = stl;

    public readonly int Ratio = 100 - idl;
}