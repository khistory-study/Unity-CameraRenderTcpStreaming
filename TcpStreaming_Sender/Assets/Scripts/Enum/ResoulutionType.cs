using UnityEngine;

public enum ResolutionType
{
    p1080,
    p720,
    p480,
    p360,
    p240,
    Custom,
}
    
public static class ResolutionTypeUtil{

    public static Vector2 GetResolution(this ResolutionType resType)
    {
        Vector2 resolution = new Vector2(1280, 720);
        switch (resType)
        {
            case ResolutionType.p1080:
                resolution.x = 1920;
                resolution.y = 1080;
                break;
            case ResolutionType.p720:
                resolution.x = 1280;
                resolution.y = 720 ;
                break;
            case ResolutionType.p480:
                resolution.x = 854;
                resolution.y = 480;
                break;
            case ResolutionType.p360:
                resolution.x = 640;
                resolution.y = 360;
                break;
            case ResolutionType.p240:
                resolution.x = 426;
                resolution.y = 240;
                break;
            default:
                resolution.x = 1280;
                resolution.y = 720 ;
                break;
        }
        return resolution;
    }
}