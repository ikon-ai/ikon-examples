allprojects {
    repositories {
        google()
        mavenCentral()
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
// Some bundled plugins pin an older compileSdk whose AndroidX dependencies now require 34+.
// Override it after the module evaluates (so we win over the plugin's own build.gradle). Reflection
// avoids needing the Android Gradle Plugin types on this script's classpath. All callbacks are
// registered before any evaluationDependsOn call: forcing :app to evaluate cascades evaluation of
// the other plugin modules, so a callback registered later in the same loop would find its module
// already evaluated and afterEvaluate would throw.
fun overrideOldCompileSdk(project: Project) {
    val androidExt = project.extensions.findByName("android") ?: return
    try {
        val current = (androidExt.javaClass.getMethod("getCompileSdkVersion").invoke(androidExt) as? String)
            ?.removePrefix("android-")?.toIntOrNull() ?: 0
        if (current < 34) {
            androidExt.javaClass.getMethod("setCompileSdkVersion", Int::class.javaPrimitiveType)
                .invoke(androidExt, 36)
        }
    } catch (_: Exception) {
    }
}

subprojects {
    if (state.executed) {
        overrideOldCompileSdk(this)
    } else {
        afterEvaluate { overrideOldCompileSdk(this) }
    }
}

subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
