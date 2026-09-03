/*
  DogPlatform.Identity - replace the existing V1 legal document contents.
  Idempotent data update: never inserts documents or creates a new version.
*/
USE [DogPlatform_IdentityDb];
GO

SET XACT_ABORT ON;
GO

DECLARE @TermsContent nvarchar(max) = N'TÉRMINOS Y CONDICIONES DE PETLIFE

Versión 1.0
Última actualización: 25/08/2026

1. INFORMACIÓN GENERAL

PetLife es una plataforma digital destinada a facilitar la gestión, cuidado y organización de información relacionada con mascotas.

La aplicación permite a sus usuarios registrar mascotas y utilizar funcionalidades relacionadas, entre otras, con perfiles, fotografías, salud, vacunación, genealogía, búsqueda de pareja, notificaciones y otros servicios que puedan incorporarse posteriormente.

Al crear una cuenta y utilizar PetLife, el usuario declara haber leído y aceptado estos Términos y Condiciones.

2. REGISTRO Y CUENTA

Para utilizar determinadas funcionalidades de PetLife es necesario crear una cuenta.

El usuario se compromete a proporcionar información veraz, actualizada y completa.

El usuario es responsable de mantener la confidencialidad de sus credenciales de acceso y de evitar su utilización por terceros no autorizados.

PetLife podrá implementar mecanismos de verificación de correo electrónico, recuperación de contraseña, autenticación y otras medidas orientadas a proteger las cuentas.

3. INFORMACIÓN SOBRE MASCOTAS

El usuario podrá registrar información relacionada con sus mascotas, incluyendo, entre otros:

- nombre;
- especie;
- raza;
- sexo;
- fecha de nacimiento;
- fotografías;
- peso;
- características físicas;
- información relacionada con vacunas;
- información genealógica;
- pedigree cuando corresponda;
- información relacionada con reproducción o búsqueda de pareja;
- otros datos relacionados con su cuidado.

El usuario declara que cuenta con autorización suficiente para registrar y administrar la información que incorpora a PetLife.

4. SALUD Y VACUNACIÓN

PetLife permite registrar información relacionada con vacunas y otros antecedentes de salud de las mascotas, así como generar recordatorios y estimaciones basadas en la información registrada.

La información proporcionada por PetLife tiene carácter informativo y de apoyo.

PetLife no sustituye la evaluación, diagnóstico, tratamiento ni recomendación de un médico veterinario.

Los calendarios de vacunación pueden variar dependiendo, entre otros factores, de la especie, edad, ubicación geográfica, condición del animal, fabricante de la vacuna y criterio veterinario.

Ante cualquier duda relacionada con la salud de una mascota, el usuario deberá consultar a un profesional veterinario.

5. RECORDATORIOS Y NOTIFICACIONES

PetLife podrá enviar recordatorios y notificaciones relacionados con vacunas, solicitudes, relaciones entre mascotas y otras funcionalidades de la plataforma.

Estos avisos constituyen herramientas auxiliares.

PetLife no garantiza que una determinada vacuna, tratamiento o procedimiento deba realizarse exactamente en la fecha indicada por la aplicación.

6. GENEALOGÍA

PetLife permite registrar y consultar relaciones genealógicas entre mascotas.

Cuando una relación involucre una mascota perteneciente a otro usuario, PetLife podrá requerir la aceptación de dicho usuario antes de activar la relación.

Los usuarios son responsables de la veracidad de la información genealógica proporcionada.

Los árboles genealógicos, estadísticas, relaciones de parentesco y demás cálculos realizados por PetLife dependen de la información disponible en la plataforma.

Estos resultados no constituyen una certificación oficial de pedigree.

7. BÚSQUEDA DE PAREJA Y REPRODUCCIÓN

PetLife puede permitir que los usuarios publiquen determinados datos de sus mascotas con la finalidad de encontrar posibles parejas reproductivas.

La información de contacto de los propietarios no deberá mostrarse inicialmente a otros usuarios, salvo aquella información que el usuario haya autorizado expresamente a compartir después de aceptar una solicitud.

Las recomendaciones, filtros, compatibilidades o advertencias proporcionadas por PetLife tienen carácter informativo.

PetLife no certifica que una mascota sea médica o genéticamente apta para reproducción.

Los usuarios deberán acudir a profesionales veterinarios y realizar las evaluaciones correspondientes antes de cualquier reproducción.

Las funciones relacionadas con posibles camadas representan únicamente intenciones o acuerdos registrados entre usuarios y no constituyen confirmación de embarazo, nacimiento o pedigree.

8. FOTOGRAFÍAS Y CONTENIDO

El usuario conserva los derechos que le correspondan sobre las fotografías y demás contenido que publique.

Al cargar contenido en PetLife, el usuario concede a la plataforma la autorización necesaria para almacenarlo, procesarlo y mostrarlo en la medida necesaria para prestar las funcionalidades solicitadas.

El usuario declara contar con los derechos o autorizaciones necesarias respecto del contenido que publique.

No deberá cargarse contenido ilegal, fraudulento, ofensivo o que vulnere derechos de terceros.

9. PRIVACIDAD ENTRE USUARIOS

PetLife procura limitar la exposición de información personal entre usuarios.

Determinadas funcionalidades, como genealogía o búsqueda de pareja, podrán permitir interacciones entre propietarios diferentes.

Los datos personales o de contacto de otro usuario solo deberán mostrarse cuando exista una finalidad legítima dentro de la funcionalidad y se hayan cumplido las condiciones de autorización correspondientes.

10. FUNCIONALIDADES FUTURAS

PetLife podrá incorporar nuevas funcionalidades, incluyendo:

- servicios relacionados con veterinarios;
- paseos y actividades;
- mascotas perdidas o encontradas;
- avisos comunitarios de ayuda;
- refugios y albergues;
- adopciones;
- donaciones voluntarias;
- herramientas adicionales relacionadas con mascotas.

Estas funcionalidades podrán contar con condiciones adicionales.

11. USO ADECUADO

El usuario se compromete a no utilizar PetLife para:

- actividades ilegales;
- fraude;
- suplantación de identidad;
- acoso;
- publicación de información falsa de forma deliberada;
- acceso no autorizado;
- manipulación de la plataforma;
- actividades que puedan perjudicar a personas, animales u otros usuarios.

PetLife podrá restringir o suspender cuentas cuando existan indicios razonables de abuso, fraude, incumplimiento de estas condiciones o riesgo para otros usuarios.

12. DISPONIBILIDAD DEL SERVICIO

PetLife procurará mantener sus servicios disponibles y operativos.

Sin embargo, no garantiza funcionamiento ininterrumpido.

Podrán existir interrupciones debido a mantenimiento, actualizaciones, fallas técnicas, proveedores externos, conectividad u otras circunstancias fuera del control razonable de la plataforma.

13. PROPIEDAD INTELECTUAL

El software, diseño, identidad visual, interfaces, logotipos, marca y demás elementos propios de PetLife se encuentran protegidos por las normas de propiedad intelectual que resulten aplicables.

El contenido perteneciente a los usuarios continuará perteneciendo a sus respectivos titulares.

14. ELIMINACIÓN DE CUENTA

El usuario podrá solicitar la eliminación de su cuenta.

PetLife eliminará o anonimizará los datos asociados cuando legal y técnicamente corresponda, excepto aquellos que deban conservarse temporalmente por obligaciones legales, seguridad, prevención de fraude, resolución de controversias u otras causas legítimas.

15. LIMITACIÓN DE RESPONSABILIDAD

PetLife proporciona herramientas tecnológicas de apoyo.

La plataforma no será responsable por decisiones veterinarias, reproductivas, comerciales o personales tomadas exclusivamente sobre la base de información mostrada por la aplicación.

Los usuarios son responsables de verificar la información relevante y consultar profesionales cuando corresponda.

16. MODIFICACIONES

PetLife podrá actualizar estos Términos y Condiciones.

Cuando se produzcan modificaciones sustanciales se informará al usuario y, cuando corresponda, se solicitará la aceptación de una nueva versión.

PetLife conserva un registro de las versiones aceptadas por cada usuario.

17. LEGISLACIÓN APLICABLE

Estos Términos y Condiciones se interpretarán conforme a la legislación vigente de la República del Perú, sin perjuicio de los derechos reconocidos a consumidores, usuarios y titulares de datos personales.';
DECLARE @PrivacyContent nvarchar(max) = N'POLÍTICA DE PRIVACIDAD DE PETLIFE

Versión 1.0
Última actualización: 25/08/2026

1. FINALIDAD DE ESTA POLÍTICA

Esta Política de Privacidad explica cómo PetLife recopila, utiliza, almacena y protege información relacionada con sus usuarios y las funcionalidades disponibles en la plataforma.

PetLife procura tratar únicamente la información necesaria para proporcionar sus servicios.

2. RESPONSABLE DEL TRATAMIENTO

Antes de publicación comercial deben completarse los siguientes datos:

Responsable:
[PENDIENTE - NOMBRE O RAZÓN SOCIAL]

RUC:
[PENDIENTE - SI CORRESPONDE]

Domicilio:
[PENDIENTE]

Correo para privacidad y ejercicio de derechos:
[PENDIENTE]

IMPORTANTE:
Mantener estos campos visibles como pendientes en Development, pero marcar claramente que deben sustituirse antes de Production.

NO inventar datos personales ni societarios.

3. INFORMACIÓN QUE PODEMOS RECOPILAR

PetLife puede tratar información proporcionada directamente por el usuario, incluyendo:

- nombre;
- apellidos;
- correo electrónico;
- teléfono;
- fotografía de perfil;
- información necesaria para autenticación y seguridad.

También puede tratar información relacionada con las mascotas registradas por el usuario, incluyendo:

- nombre;
- especie;
- raza;
- sexo;
- fecha de nacimiento;
- peso;
- características físicas;
- fotografías;
- vacunas;
- información de salud registrada;
- genealogía;
- pedigree;
- información relacionada con búsqueda de pareja;
- posibles intenciones de camada;
- otra información relacionada con el uso de las funcionalidades solicitadas.

4. FINALIDADES DEL TRATAMIENTO

La información puede utilizarse para:

- crear y administrar cuentas;
- autenticar usuarios;
- verificar identidad o correo cuando corresponda;
- administrar perfiles;
- registrar y gestionar mascotas;
- almacenar fotografías;
- gestionar información de salud y vacunación;
- generar recordatorios;
- enviar notificaciones;
- construir árboles genealógicos;
- gestionar solicitudes entre propietarios;
- proporcionar funciones de búsqueda de pareja;
- permitir compartir datos de contacto cuando exista autorización;
- garantizar seguridad de la plataforma;
- detectar errores, abuso o fraude;
- prestar soporte;
- proporcionar otras funcionalidades solicitadas por el usuario.

5. CONSENTIMIENTO

Cuando el tratamiento requiera consentimiento, PetLife solicitará una manifestación previa y expresa del usuario.

PetLife mantiene información sobre:

- documento aceptado;
- versión;
- fecha y hora de aceptación.

Una nueva versión podrá requerir aceptación adicional cuando corresponda.

6. INFORMACIÓN DE CONTACTO ENTRE USUARIOS

PetLife aplica restricciones destinadas a proteger la información personal entre usuarios.

Por ejemplo, en funcionalidades de búsqueda de pareja, la información de contacto del propietario de una mascota no se muestra inicialmente.

Cuando dos usuarios aceptan una solicitud, podrán compartirse únicamente los datos que cada usuario haya autorizado.

PetLife no deberá utilizar identificadores internos para permitir acceso arbitrario a información privada de otros usuarios.

7. GENEALOGÍA

Cuando una relación genealógica involucre mascotas pertenecientes a diferentes usuarios, PetLife podrá utilizar mecanismos de solicitud y aceptación.

La existencia de una mascota registrada por otra persona no debe permitir consultar libremente la información personal de su propietario.

8. SERVICIOS Y PROVEEDORES

PetLife podrá utilizar proveedores tecnológicos para operar la plataforma.

Estos proveedores pueden incluir servicios de:

- infraestructura y alojamiento;
- almacenamiento;
- bases de datos;
- correo electrónico;
- notificaciones;
- mapas;
- monitoreo;
- seguridad;
- otros servicios técnicos necesarios.

Antes del lanzamiento de producción deberán documentarse los proveedores efectivamente utilizados.

Cuando corresponda, PetLife informará sobre transferencias nacionales o internacionales de información conforme a la normativa aplicable.

9. SEGURIDAD

PetLife implementa medidas técnicas y organizativas destinadas a reducir riesgos de:

- acceso no autorizado;
- alteración;
- pérdida;
- divulgación;
- destrucción de información.

Estas medidas pueden incluir:

- autenticación;
- control de acceso;
- cifrado de comunicaciones;
- gestión segura de credenciales;
- separación de servicios;
- registro de eventos;
- restricciones de acceso a bases de datos.

Ningún sistema informático puede garantizar seguridad absoluta.

10. CONSERVACIÓN

La información será conservada durante el tiempo necesario para prestar los servicios solicitados y cumplir las finalidades informadas.

Cuando corresponda eliminar información, esta podrá ser eliminada o anonimizada, salvo que exista una razón legal o legítima que justifique su conservación.

11. DERECHOS DEL USUARIO

El usuario podrá ejercer los derechos reconocidos por la normativa peruana sobre protección de datos personales.

Entre ellos se encuentran los derechos de:

Acceso:
conocer la información personal tratada y las finalidades correspondientes.

Rectificación:
solicitar la corrección o actualización de información inexacta o incompleta.

Cancelación:
solicitar la cancelación del tratamiento cuando corresponda.

Oposición:
oponerse a determinados tratamientos cuando legalmente proceda.

Las solicitudes podrán dirigirse al canal de privacidad indicado por PetLife.

El correo definitivo deberá configurarse antes del lanzamiento de producción.

12. ELIMINACIÓN DE CUENTA

PetLife permitirá solicitar la eliminación de una cuenta.

Cuando corresponda, los datos personales serán eliminados o anonimizados, excepto aquellos que deban mantenerse temporalmente por obligaciones legales, seguridad, prevención de fraude u otras razones legítimas.

13. INFORMACIÓN DE MENORES

PetLife no está diseñada para que menores que no puedan proporcionar válidamente los consentimientos correspondientes administren cuentas de forma independiente.

Antes del lanzamiento de producción deberá definirse formalmente la edad mínima de uso de la plataforma.

14. CAMBIOS EN ESTA POLÍTICA

PetLife podrá actualizar esta Política de Privacidad debido a cambios funcionales, tecnológicos o regulatorios.

Cuando exista una modificación sustancial, PetLife podrá requerir la aceptación de una nueva versión.

15. AUTORIDAD DE PROTECCIÓN DE DATOS

En Perú, la autoridad competente en materia de protección de datos personales es la Autoridad Nacional de Protección de Datos Personales.

Los usuarios podrán acudir a los mecanismos establecidos legalmente cuando consideren afectados sus derechos.';

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE auth.LegalDocuments
    SET Content = @TermsContent
    WHERE Type = N'TermsAndConditions'
      AND Version = N'1.0';

    IF @@ROWCOUNT <> 1
        THROW 51000, 'Expected exactly one TermsAndConditions 1.0 document.', 1;

    UPDATE auth.LegalDocuments
    SET Content = @PrivacyContent
    WHERE Type = N'PrivacyPolicy'
      AND Version = N'1.0';

    IF @@ROWCOUNT <> 1
        THROW 51001, 'Expected exactly one PrivacyPolicy 1.0 document.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
