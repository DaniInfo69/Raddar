using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Avisen.Controls;
using Avisen.Models;
using Microsoft.Maui.Maps.Handlers;
using System.Diagnostics; // <-- para Debug


public class CustomMapHandler : MapHandler
{
    private static new IPropertyMapper<CustomMap, CustomMapHandler> Mapper =
        new PropertyMapper<CustomMap, CustomMapHandler>(MapHandler.Mapper)
        {
            [nameof(CustomMap.CustomPins)] = MapPins
        };

    private GoogleMap _googleMap; // Almacenar referencia a GoogleMap

    public CustomMapHandler() : base(Mapper)
    {
        Debug.WriteLine("[CustomMapHandler] Constructor creado.");
    }

    protected override void ConnectHandler(MapView platformView)
    {
        Debug.WriteLine("[CustomMapHandler] ConnectHandler llamado. platformView: " + (platformView == null ? "null" : "ok"));
        base.ConnectHandler(platformView);
        platformView.GetMapAsync(new MapCallback(this));
    }

    private static void MapPins(CustomMapHandler handler, CustomMap map)
    {
        Debug.WriteLine("[CustomMapHandler] MapPins mapper invocado (firma corregida).");
        if (handler == null)
        {
            Debug.WriteLine("[CustomMapHandler] handler es null en MapPins.");
            return;
        }

        Debug.WriteLine($"[CustomMapHandler] MarkerMap contiene {handler.MarkerMap.Count} items antes de limpiar.");
        try
        {
            foreach (var kv in handler.MarkerMap.Values)
            {
                var marker = kv.Marker;
                if (marker != null)
                {
                    Debug.WriteLine($"[CustomMapHandler] Eliminando marker con id: {marker.Id}");
                    marker.Remove();
                }
            }
            handler.MarkerMap.Clear();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[CustomMapHandler] Error al limpiar MarkerMap: " + ex);
        }

        handler.AddPins();
    }

    private void AddPins()
    {
        Debug.WriteLine("[CustomMapHandler] AddPins llamado.");
        if (_googleMap == null)
        {
            Debug.WriteLine("[CustomMapHandler] _googleMap es null. No se pueden añadir pins todavía.");
            return;
        }

        if (VirtualView is CustomMap customMap)
        {
            if (customMap.CustomPins == null)
            {
                Debug.WriteLine("[CustomMapHandler] customMap.CustomPins es null. No hay pins para agregar.");
                return;
            }

            Debug.WriteLine($"[CustomMapHandler] customMap.CustomPins contiene {customMap.CustomPins.Count} pins. Iterando...");
            foreach (var pin in customMap.CustomPins)
            {
                try
                {
                    Debug.WriteLine($"[CustomMapHandler] Procesando pin Id={pin?.Id} Pos=({pin?.Position?.Latitude},{pin?.Position?.Longitude}) Icon={pin?.Icon}");
                    var opts = new MarkerOptions()
                        .SetPosition(new LatLng(pin.Position.Latitude, pin.Position.Longitude));

                    var icon = GetIcon(pin.Icon);
                    if (icon != null)
                    {
                        opts.SetIcon(icon);
                        Debug.WriteLine($"[CustomMapHandler] Icono obtenido para {pin.Icon}");
                    }
                    else
                    {
                        Debug.WriteLine($"[CustomMapHandler] No se obtuvo icono para {pin.Icon}. Usando icono por defecto.");
                    }

                    var marker = _googleMap.AddMarker(opts);
                    if (marker != null)
                    {
                        Debug.WriteLine($"[CustomMapHandler] Marker agregado con id: {marker.Id}");
                        MarkerMap[marker.Id] = (marker, pin);
                    }
                    else
                    {
                        Debug.WriteLine("[CustomMapHandler] _googleMap.AddMarker devolvió null.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[CustomMapHandler] Error al agregar pin: " + ex);
                }
            }
        }
        else
        {
            Debug.WriteLine("[CustomMapHandler] VirtualView no es CustomMap.");
        }
    }

    private BitmapDescriptor GetIcon(string iconName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(iconName))
            {
                Debug.WriteLine("[CustomMapHandler] GetIcon: iconName null o vacío.");
                return null;
            }

            int resId = Context.Resources.GetIdentifier(iconName, "drawable", Context.PackageName);
            Debug.WriteLine($"[CustomMapHandler] GetIcon: resId para '{iconName}' = {resId}");
            if (resId == 0)
            {
                Debug.WriteLine($"[CustomMapHandler] Recurso drawable '{iconName}' no encontrado.");
                return null;
            }

            var bmp = BitmapFactory.DecodeResource(Context.Resources, resId);
            if (bmp == null)
            {
                Debug.WriteLine($"[CustomMapHandler] BitmapFactory.DecodeResource devolvió null para resId {resId}.");
                return null;
            }

            var scaled = Bitmap.CreateScaledBitmap(bmp, 180, 180, false);
            bmp.Recycle();

            return BitmapDescriptorFactory.FromBitmap(scaled);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[CustomMapHandler] Excepción en GetIcon: " + ex);
            return null;
        }
    }

    // Mapa auxiliar de marcador=>MapPin para manejar clicks
    public Dictionary<string, (Marker Marker, MapPin Pin)> MarkerMap { get; }
        = new Dictionary<string, (Marker, MapPin)>();

    private class MapCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        private readonly CustomMapHandler _handler;

        public MapCallback(CustomMapHandler handler)
        {
            _handler = handler;
            Debug.WriteLine("[CustomMapHandler.MapCallback] Callback creado.");
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            Debug.WriteLine("[CustomMapHandler.MapCallback] OnMapReady llamado.");
            _handler._googleMap = googleMap;

            // INFO extra
            try
            {
                if (_handler.VirtualView is CustomMap v)
                {
                    Debug.WriteLine($"[CustomMapHandler.MapCallback] VirtualView es CustomMap. CustomPins == null? {v.CustomPins == null}");
                    if (v.CustomPins != null)
                        Debug.WriteLine($"[CustomMapHandler.MapCallback] CustomPins.Count = {v.CustomPins.Count}");
                }
                else
                {
                    Debug.WriteLine("[CustomMapHandler.MapCallback] VirtualView no es CustomMap o es null.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[CustomMapHandler.MapCallback] Error al leer VirtualView: " + ex);
            }

            googleMap.MarkerClick += (s, args) =>
            {
                Debug.WriteLine("[CustomMapHandler] MarkerClick invocado. Marker id: " + args.Marker?.Id);
                if (args?.Marker != null && _handler.MarkerMap.TryGetValue(args.Marker.Id, out var info))
                {
                    Debug.WriteLine("[CustomMapHandler] Ejecutando ClickedCommand del pin Id: " + info.Pin.Id);
                    info.Pin.ClickedCommand?.Execute(null);
                }
                else
                {
                    Debug.WriteLine("[CustomMapHandler] Marker no encontrado en MarkerMap.");
                }
            };

            Debug.WriteLine("[CustomMapHandler] Forzando UpdateValue de CustomPins.");
            _handler.UpdateValue(nameof(CustomMap.CustomPins));
        }

    }
}
