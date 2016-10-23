using System;

namespace VisualisationLib
{
    public struct Vector
    {
        public double X;
        public double Y;

        public Vector(double xp, double yp)
        {
            X = xp;
            Y = yp;
        }

        public static Vector operator +(Vector vec1, Vector vec2)
        {
            return new Vector(vec1.X + vec2.X, vec1.Y + vec2.Y);
        }

        public static Vector operator -(Vector vec1, Vector vec2)
        {
            return new Vector(vec1.X - vec2.X, vec1.Y - vec2.Y);
        }

        public static Vector operator *(double p, Vector vec)
        {
            return new Vector(p * vec.X, p * vec.Y);
        }

        public static Vector operator /(Vector vec, double p)
        {
            return new Vector(vec.X / p, vec.Y / p);
        }

        public override string ToString()
        {
            return "x = " + X + ", y = " + Y;
        }
    }

    public class ClimateModel
    {
        public int Id { get; set; }
        public DateTime ModelDate { get; set; }
        public string ModelDescription { get; set; }
        public ParticleTrack ModelData { get; set; }
    }
}
