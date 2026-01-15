export default function FAQ() {
  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">
        Frequently Asked Questions
      </h1>

      <div className="prose dark:prose-invert max-w-none">
        <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">
          Last updated: January 14, 2026
        </p>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Getting Started
          </h2>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              What is TrueDope?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              TrueDope is a precision shooting data logging platform designed to help shooters
              track and analyze their shooting data. Record DOPE (Data on Previous Engagements),
              chronograph readings, group measurements, and more to improve your long-range
              shooting performance.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              How do I create an account?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              You can create an account by downloading the iOS app or visiting our web
              application. Click "Register" and provide your email address and a secure
              password. You'll be ready to start logging data immediately.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              How do I add my first rifle?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              After logging in, navigate to the Rifles section and tap "Add Rifle." Enter
              your rifle's details including make, model, caliber, barrel length, and twist
              rate. You can also add optic information and notes about your setup.
            </p>
          </div>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Recording Data
          </h2>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              What is a session?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              A session represents a single range visit or shooting event. Each session is
              linked to a specific rifle and can contain multiple DOPE entries, chronograph
              readings, and group measurements. Sessions also capture weather conditions and
              location data.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              What is DOPE data?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              DOPE stands for "Data on Previous Engagements." It records the elevation and
              windage adjustments you made at specific distances. This data helps you build
              accurate drop charts and predict adjustments needed at various ranges.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              How do I log chronograph readings?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              Within a session, you can add chronograph entries to record muzzle velocity
              data. Enter individual shot velocities or summary statistics like average
              velocity, standard deviation, and extreme spread. This data helps track
              ammunition consistency and load development.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              How do I record group measurements?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              Add group entries to a session to track accuracy. Record group size (in MOA
              or inches), shot count, and distance. You can also upload photos of your
              targets for reference.
            </p>
          </div>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Managing Equipment
          </h2>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              How do I manage ammunition and lots?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              The Ammunition section lets you catalog your ammo by manufacturer, caliber,
              bullet weight, and type. Within each ammunition entry, you can create lots
              to track specific batches, lot numbers, purchase dates, and round counts.
              This helps identify which ammunition performs best in your rifles.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              Can I save shooting locations?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              Yes! The Locations feature lets you save your favorite ranges and shooting
              spots. Store GPS coordinates, elevation, and notes about each location. When
              creating a session, you can quickly select from your saved locations.
            </p>
          </div>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Data & Privacy
          </h2>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              Where is my data stored?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              Your data is stored securely on our servers using industry-standard encryption.
              All data transmission uses HTTPS, and your password is securely hashed. We
              never share or sell your personal information. See our{' '}
              <a href="/privacy" className="text-blue-600 dark:text-blue-400 hover:underline">
                Privacy Policy
              </a>{' '}
              for complete details.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              Can I export my data?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              Yes, you can export your shooting data from the Settings page. Your data
              belongs to you, and we make it easy to download a complete copy of your
              records at any time.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              How do I delete my account?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              You can delete your account from the Settings page. Account deletion will
              permanently remove all your data from our servers within 30 days. This
              action cannot be undone.
            </p>
          </div>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Premium Features
          </h2>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              What's included in the free tier?
            </h3>
            <p className="text-gray-700 dark:text-gray-300 mb-4">
              The free tier includes full access to core features:
            </p>
            <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 space-y-1">
              <li>Unlimited rifles and ammunition entries</li>
              <li>Session logging with DOPE, chronograph, and group data</li>
              <li>Location management</li>
              <li>Basic analytics and charts</li>
              <li>Cross-device sync between iOS and web</li>
            </ul>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              What additional features are available?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              Premium features include advanced analytics, extended data retention,
              priority support, and more. Check the app for current subscription options
              and pricing.
            </p>
          </div>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Technical Questions
          </h2>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              Which platforms are supported?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              TrueDope is available as a native iOS app (requires iOS 17.0 or later) and
              as a web application accessible from any modern browser. Your data syncs
              automatically between platforms.
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
              Does the app work offline?
            </h3>
            <p className="text-gray-700 dark:text-gray-300">
              The iOS app caches your data for offline viewing. However, creating new
              entries and syncing changes requires an internet connection. We recommend
              syncing before heading to the range if you expect limited connectivity.
            </p>
          </div>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Contact Us
          </h2>
          <p className="text-gray-700 dark:text-gray-300">
            Have a question that's not answered here? Contact us at{' '}
            <a
              href="mailto:support@truedope.io"
              className="text-blue-600 dark:text-blue-400 hover:underline"
            >
              support@truedope.io
            </a>
            .
          </p>
        </section>
      </div>
    </div>
  );
}
