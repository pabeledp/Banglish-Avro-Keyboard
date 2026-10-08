import Cocoa
import InputMethodKit

let kConnectionName = Bundle.main.infoDictionary?["InputMethodConnectionName"] as? String ?? "org.banglish.inputmethod.Banglish_Connection"
let kBundleId = Bundle.main.bundleIdentifier ?? "org.banglish.inputmethod.Banglish"
var server: IMKServer!

autoreleasepool {
    server = IMKServer(name: kConnectionName, bundleIdentifier: kBundleId)

    let delegate = BanglishAppCoordinator()
    NSApplication.shared.delegate = delegate

    withExtendedLifetime(delegate) {
        NSApplication.shared.run()
    }
}
