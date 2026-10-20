using System.Net;

namespace ProyectoClubCreativo.Services
{
    public static class PlantillaCorreo
    {
        private const string ColorPrimario = "#7B2CBF";
        private const string ColorSecundario = "#FF007F";
        private const string ColorBotonHover = "#9D4EDD";
        private const string ColorFondo = "#F4F0F9";
        private const string ColorTexto = "#2D0C4E";
        public static string Construir(
            string titulo,
            string cuerpoHtml,
            string? textoBoton = null,
            string? urlBoton = null,
            string? aviso = null,
            string? urlBase = null,
            string? preheader = null)
        {
            bool puedeMostrarLogo =
                !string.IsNullOrWhiteSpace(urlBase) &&
                !urlBase.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
                !urlBase.Contains("127.0.0.1");
            string baseUrlLimpia = (urlBase ?? "https://www.clubcreativomivo.com").TrimEnd('/');
            string encabezadoLogo = puedeMostrarLogo
                ? $@"<img src=""{baseUrlLimpia}/images/logo.jpg""
                          alt=""Club Creativo MiVo"" width=""100"" height=""100""
                          style=""display:block;margin:0 auto 12px;border:3px solid #ffffff;border-radius:50%;box-shadow:0 4px 10px rgba(0,0,0,0.15);"">"
                : "";
            string boton = string.IsNullOrWhiteSpace(textoBoton) || string.IsNullOrWhiteSpace(urlBoton)
                ? ""
                : $@"
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:32px 0 20px;"">
  <tr>
    <td align=""center"">
      <!--[if mso]>
      <v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href=""{urlBoton}"" style=""height:48px;v-text-anchor:middle;width:240px;"" arcsize=""20%"" stroke=""f"" fillcolor=""{ColorSecundario}"">
        <w:anchorlock/>
        <center style=""color:#ffffff;font-family:Arial,sans-serif;font-size:16px;font-weight:bold;"">{WebUtility.HtmlEncode(textoBoton)}</center>
      </v:roundrect>
      <![endif]-->
      <!--[if !mso]><!-->
      <a href=""{urlBoton}""
         style=""display:inline-block;padding:15px 32px;background-color:{ColorSecundario};background-image:linear-gradient(135deg, {ColorSecundario} 0%, #FF5400 100%);color:#ffffff;font-family:'Segoe UI',Arial,sans-serif;font-size:16px;font-weight:bold;text-decoration:none;border-radius:30px;box-shadow:0 4px 12px rgba(255,0,127,0.35);transition:all 0.3s ease;"">
        {WebUtility.HtmlEncode(textoBoton)}
      </a>
      <!--<![endif]-->
    </td>
  </tr>
</table>";
            string bloqueAviso = string.IsNullOrWhiteSpace(aviso)
                ? ""
                : $@"
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:20px 0 10px;"">
  <tr>
    <td style=""background-color:#FFF4F8;border-left:5px solid {ColorSecundario};border-radius:8px;padding:16px 20px;"">
      <p style=""margin:0;color:#5A3E56;font-size:14px;line-height:1.5;"">
        <strong>Nota:</strong> {aviso}
      </p>
    </td>
  </tr>
</table>";
            return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <meta http-equiv=""X-UA-Encoding"" content=""IE=edge"">
  <title>{WebUtility.HtmlEncode(titulo)}</title>
</head>
<body style=""margin:0;padding:0;background-color:{ColorFondo};font-family:'Segoe UI',Arial,Helvetica,sans-serif;-webkit-font-smoothing:antialiased;"">
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;font-size:1px;line-height:1px;color:#ffffff;"">
    {WebUtility.HtmlEncode(preheader ?? titulo)}
  </div>
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:{ColorFondo};padding:30px 10px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background-color:#ffffff;border-radius:20px;overflow:hidden;box-shadow:0 10px 25px rgba(123,44,191,0.08);"">
          <tr>
            <td height=""6"" style=""background: linear-gradient(90deg, #FF007F 0%, #7B2CBF 50%, #00F5D4 100%); font-size:0; line-height:0;"">&nbsp;</td>
          </tr>
          <tr>
            <td align=""center"" style=""background-color:{ColorPrimario};background-image:linear-gradient(135deg, {ColorPrimario} 0%, #5A189A 50%, #3C096C 100%);padding:35px 20px 30px;"">
              {encabezadoLogo}
              <div style=""font-family:'Segoe UI',Arial,sans-serif;font-size:30px;font-weight:800;color:#ffffff;letter-spacing:0.5px;text-shadow:0 2px 4px rgba(0,0,0,0.2);"">
                Club Creativo <span style=""color:#00F5D4;"">MiVo</span>
              </div>
              <div style=""margin-top:6px;font-size:14px;color:#E0AAFF;font-weight:500;letter-spacing:1px;text-transform:uppercase;"">
                ✨ Comunidad Creativa & Emprendimiento ✨
              </div>
            </td>
          </tr>
          <tr>
            <td style=""padding:40px 35px 30px;background-color:#ffffff;"">
              <h1 style=""margin:0 0 20px;color:{ColorTexto};font-size:24px;font-weight:700;line-height:1.3;"">
                {WebUtility.HtmlEncode(titulo)}
              </h1>
              
              <div style=""font-size:16px;line-height:1.7;color:#4A3E56;"">
                {cuerpoHtml}
              </div>

              {boton}
              {bloqueAviso}
            </td>
          </tr>
          <tr>
            <td align=""center"" style=""padding:0 35px;"">
              <div style=""border-top:2px dashed #E9ECEF;height:1px;width:100%;""></div>
            </td>
          </tr>
          <tr>
            <td align=""center"" style=""background-color:#FAF7FC;padding:28px 20px;text-align:center;"">
              <p style=""margin:0 0 8px;color:{ColorPrimario};font-size:15px;font-weight:700;"">
                🎨 Club Creativo MiVo
              </p>
              <p style=""margin:0 0 15px;color:#7A6E85;font-size:13px;"">
                Impulsando la creatividad, el diseño y el emprendimiento.
              </p>
              <p style=""margin:0 0 18px;font-size:13px;"">
                <a href=""{baseUrlLimpia}"" style=""color:{ColorSecundario};text-decoration:none;font-weight:bold;margin:0 8px;"">Visitar Sitio Web</a> •
                <a href=""{baseUrlLimpia}#contacto"" style=""color:{ColorPrimario};text-decoration:none;font-weight:bold;margin:0 8px;"">Contacto</a>
              </p>
              <p style=""margin:0;color:#B0A3BC;font-size:11px;"">
                © {DateTime.Now.Year} Club Creativo MiVo. Todos los derechos reservados.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
        public static string E(string? texto) => WebUtility.HtmlEncode(texto ?? "");
    }
}