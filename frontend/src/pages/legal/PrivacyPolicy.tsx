export default function PrivacyPolicy() {
  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-8">
        Privacy Policy
      </h1>

      <div className="prose dark:prose-invert max-w-none">
        <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">
          Last updated: December 26, 2024
        </p>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Introduction
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            TrueDope ("we," "our," or "us") is a precision shooting data logging application.
            This Privacy Policy explains how we collect, use, disclose, and safeguard your
            information when you use our mobile application and web service.
          </p>
          <p className="text-gray-700 dark:text-gray-300">
            Please read this privacy policy carefully. By using TrueDope, you agree to the
            collection and use of information in accordance with this policy.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Information We Collect
          </h2>

          <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
            Account Information
          </h3>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            When you create an account, we collect:
          </p>
          <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 mb-4 space-y-1">
            <li>Email address</li>
            <li>Name (optional)</li>
            <li>Password (securely hashed, never stored in plain text)</li>
          </ul>

          <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
            Shooting Data
          </h3>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            To provide our core functionality, we store:
          </p>
          <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 mb-4 space-y-1">
            <li>Rifle and equipment specifications</li>
            <li>Ammunition details and lot information</li>
            <li>Range session data (DOPE, chronograph readings, group measurements)</li>
            <li>Weather conditions during sessions</li>
            <li>Session notes and observations</li>
          </ul>

          <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
            Location Data
          </h3>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            With your permission, we collect:
          </p>
          <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 mb-4 space-y-1">
            <li>GPS coordinates for saved range locations</li>
            <li>Location data to fetch accurate weather information</li>
          </ul>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            Location access is optional and only used when you explicitly request weather
            data or save a location.
          </p>

          <h3 className="text-lg font-medium text-gray-800 dark:text-gray-200 mb-2">
            Photos and Images
          </h3>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            You may optionally upload images of:
          </p>
          <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 mb-4 space-y-1">
            <li>Rifle setups</li>
            <li>Target groups</li>
            <li>Range session documentation</li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            How We Use Your Information
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            We use your information solely to:
          </p>
          <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 mb-4 space-y-1">
            <li>Provide and maintain the TrueDope service</li>
            <li>Store and sync your shooting data across devices</li>
            <li>Generate analytics and charts from your data</li>
            <li>Fetch weather data for your locations</li>
            <li>Send important service-related communications</li>
            <li>Improve and optimize our application</li>
          </ul>
          <p className="text-gray-700 dark:text-gray-300 font-medium">
            We do not sell, trade, or rent your personal information to third parties.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Data Storage and Security
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            Your data is stored securely using industry-standard practices:
          </p>
          <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 mb-4 space-y-1">
            <li>All data is transmitted over HTTPS encryption</li>
            <li>Passwords are hashed using secure algorithms</li>
            <li>Authentication uses JWT tokens with secure expiration</li>
            <li>Database access is restricted and monitored</li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Third-Party Services
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            TrueDope uses the following third-party services:
          </p>
          <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 mb-4 space-y-1">
            <li>
              <strong>OpenWeatherMap</strong> - To fetch weather data for your locations.
              Only coordinates are shared, not personal information.
            </li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Your Rights
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            You have the right to:
          </p>
          <ul className="list-disc list-inside text-gray-700 dark:text-gray-300 mb-4 space-y-1">
            <li>Access your personal data</li>
            <li>Correct inaccurate data</li>
            <li>Delete your account and associated data</li>
            <li>Export your data</li>
            <li>Withdraw consent for optional features (like location access)</li>
          </ul>
          <p className="text-gray-700 dark:text-gray-300">
            To exercise these rights, please contact us at the email address below.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Data Retention
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            We retain your data for as long as your account is active. If you delete your
            account, we will delete your personal data within 30 days, except where we are
            required to retain it for legal purposes.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Children's Privacy
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            TrueDope is not intended for use by children under the age of 13. We do not
            knowingly collect personal information from children under 13.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Changes to This Policy
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            We may update this privacy policy from time to time. We will notify you of any
            changes by posting the new privacy policy on this page and updating the "Last
            updated" date.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Contact Us
          </h2>
          <p className="text-gray-700 dark:text-gray-300 mb-4">
            If you have questions about this privacy policy or our data practices, please
            contact us at:
          </p>
          <p className="text-gray-700 dark:text-gray-300">
            <strong>Email:</strong> privacy@truedope.io
          </p>
        </section>
      </div>
    </div>
  );
}
