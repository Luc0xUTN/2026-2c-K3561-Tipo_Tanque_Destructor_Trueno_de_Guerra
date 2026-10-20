using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Utils;

public static class UI
{
    public static List<Vector2> GenerateElementsPosition(Vector2 basePosition, int paddingX, int paddingY, int numberElements)
    {
        if (numberElements == 1)
        {
            var outPosition = new List<Vector2>();
            outPosition.Add(basePosition);
            return outPosition;
        }
        var prevPosition = GenerateElementsPosition(basePosition, paddingX, paddingY, numberElements - 1);
        prevPosition.Add(new Vector2(basePosition.X + paddingX * (numberElements - 1) , basePosition.Y + paddingY * (numberElements - 1)));
        return prevPosition;
    }
        
        
        



}