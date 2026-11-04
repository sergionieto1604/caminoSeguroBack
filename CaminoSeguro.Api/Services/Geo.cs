namespace CaminoSeguro.Api.Services;

public static class Geo
{
    private const double RadioTierraKm = 6371.0;

    /// <summary>Distancia en km entre dos puntos (fórmula de Haversine).</summary>
    public static double DistanciaKm(double lat1, double lng1, double lat2, double lng2)
    {
        double dLat = ARadianes(lat2 - lat1);
        double dLng = ARadianes(lng2 - lng1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ARadianes(lat1)) * Math.Cos(ARadianes(lat2)) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return RadioTierraKm * c;
    }

    private static double ARadianes(double grados) => grados * Math.PI / 180.0;
}
