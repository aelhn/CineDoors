namespace CineDoors.Core.Entities;
public enum MovieStatus // liste des 2 états d'un film/série, pas une classe. L'avoir en liste fermée évite les fautes de frappes. (utilisé dans "MovieTracking.cs")
{
    ToWatch, 
    Watched
}
