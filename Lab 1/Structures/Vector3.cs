public struct Vector3
{
    public float X, Y, Z, W;

    public Vector3(float x, float y, float z, float w = 1f)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector3 operator +(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector3 operator *(Vector3 v, float scalar)
    {
        return new Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar, v.W * scalar);
    }

    public static Vector3 Zero()
    {
        return new Vector3(0, 0, 0);
    }

    public static Vector3 VectorMultiplication(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    public static float ScalarMultiplication(Vector3 a, Vector3 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public float Length()
    {
        return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
    }

    public Vector3 Normalize()
    {
        float length = Length();
        if (length > 0)
            return this * (1.0f / length);
        return this;
    }
    public static Vector3 Normalize(Vector3 v)
    {
        return v.Normalize();
    }

  
    public static Vector3[] Normalize(params Vector3[] vectors)
    {
        Vector3[] normalizedVectors = new Vector3[vectors.Length];
        for (int i = 0; i < vectors.Length; i++)
        {
            normalizedVectors[i] = vectors[i].Normalize();
        }
        return normalizedVectors;
    }

    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        // Ограничим t от 0 до 1 для корректности
        t = Math.Clamp(t, 0f, 1f);

        return new Vector3(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t
        );
    }

    public static Vector3 operator /(Vector3 a, float b)
    {
        return new Vector3(a.X/b, a.Y/b, a.Z/b);
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    // Квадрат расстояния для оптимизации (не вычисляет корень)
    public static float DistanceSquared(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}
