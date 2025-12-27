using Prometheus;

namespace TrueDope.Api.Services;

/// <summary>
/// Custom Prometheus metrics for TrueDope business events.
/// These metrics provide visibility into actual application usage beyond standard HTTP metrics.
/// </summary>
public static class TrueDopeMetrics
{
    // ==================== Session Metrics ====================

    private static readonly Counter SessionsCreated = Metrics
        .CreateCounter(
            "truedope_sessions_created_total",
            "Total number of shooting sessions created");

    private static readonly Counter DopeEntriesCreated = Metrics
        .CreateCounter(
            "truedope_dope_entries_created_total",
            "Total number of DOPE entries logged");

    private static readonly Counter ChronoSessionsCreated = Metrics
        .CreateCounter(
            "truedope_chrono_sessions_created_total",
            "Total number of chronograph sessions logged");

    private static readonly Counter VelocityReadingsCreated = Metrics
        .CreateCounter(
            "truedope_velocity_readings_created_total",
            "Total number of velocity readings recorded");

    private static readonly Counter GroupEntriesCreated = Metrics
        .CreateCounter(
            "truedope_group_entries_created_total",
            "Total number of shot groups logged");

    // ==================== Equipment Metrics ====================

    private static readonly Counter RiflesCreated = Metrics
        .CreateCounter(
            "truedope_rifles_created_total",
            "Total number of rifles added");

    private static readonly Counter AmmunitionCreated = Metrics
        .CreateCounter(
            "truedope_ammunition_created_total",
            "Total number of ammunition records created");

    private static readonly Counter LocationsCreated = Metrics
        .CreateCounter(
            "truedope_locations_created_total",
            "Total number of shooting locations created");

    // ==================== Image Metrics ====================

    private static readonly Counter ImagesUploaded = Metrics
        .CreateCounter(
            "truedope_images_uploaded_total",
            "Total number of images uploaded",
            new CounterConfiguration
            {
                LabelNames = new[] { "parent_type" }
            });

    private static readonly Counter ImageBytesUploaded = Metrics
        .CreateCounter(
            "truedope_image_bytes_uploaded_total",
            "Total bytes of images uploaded");

    // ==================== Auth Metrics ====================

    private static readonly Counter LoginsSuccessful = Metrics
        .CreateCounter(
            "truedope_logins_successful_total",
            "Total number of successful logins");

    private static readonly Counter LoginsFailed = Metrics
        .CreateCounter(
            "truedope_logins_failed_total",
            "Total number of failed login attempts");

    private static readonly Counter Registrations = Metrics
        .CreateCounter(
            "truedope_registrations_total",
            "Total number of new user registrations");

    private static readonly Counter PasswordResets = Metrics
        .CreateCounter(
            "truedope_password_resets_total",
            "Total number of password reset completions");

    // ==================== Initialization ====================

    /// <summary>
    /// Initializes all metrics with zero values so they appear immediately in /metrics output.
    /// Call this at application startup.
    /// </summary>
    public static void Initialize()
    {
        // Touch each counter to ensure it's published with initial value of 0
        SessionsCreated.Inc(0);
        DopeEntriesCreated.Inc(0);
        ChronoSessionsCreated.Inc(0);
        VelocityReadingsCreated.Inc(0);
        GroupEntriesCreated.Inc(0);
        RiflesCreated.Inc(0);
        AmmunitionCreated.Inc(0);
        LocationsCreated.Inc(0);
        ImageBytesUploaded.Inc(0);
        LoginsSuccessful.Inc(0);
        LoginsFailed.Inc(0);
        Registrations.Inc(0);
        PasswordResets.Inc(0);
    }

    // ==================== Public Methods ====================

    // Sessions
    public static void RecordSessionCreated() => SessionsCreated.Inc();
    public static void RecordDopeEntry() => DopeEntriesCreated.Inc();
    public static void RecordChronoSession() => ChronoSessionsCreated.Inc();
    public static void RecordVelocityReading() => VelocityReadingsCreated.Inc();
    public static void RecordGroupEntry() => GroupEntriesCreated.Inc();

    // Equipment
    public static void RecordRifleCreated() => RiflesCreated.Inc();
    public static void RecordAmmunitionCreated() => AmmunitionCreated.Inc();
    public static void RecordLocationCreated() => LocationsCreated.Inc();

    // Images
    public static void RecordImageUploaded(string parentType, long bytes)
    {
        ImagesUploaded.WithLabels(parentType.ToLowerInvariant()).Inc();
        ImageBytesUploaded.Inc(bytes);
    }

    // Auth
    public static void RecordLoginSuccessful() => LoginsSuccessful.Inc();
    public static void RecordLoginFailed() => LoginsFailed.Inc();
    public static void RecordRegistration() => Registrations.Inc();
    public static void RecordPasswordReset() => PasswordResets.Inc();
}
