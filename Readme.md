ModuleAnimateActiveSecondary

This module will control a secondary animation in a part.  That secondary animation may have additional transforms which shouldn't be visible if the required resource is not available.
If the part is deployable using a ModuleAnimationGroup, the optional parameter:
	requireDeploy 
will control whether the animation will be visible only when the part is deployed
If a resource is needed, then when that resource is NOT available, the animation will stop
Transforms which should be hidden while the animation is disabled is specified using the flameoutHideTransform

To display debugging info in the log, set debug = true

To minimize overhead, the resource availability is checked once a second


Definition
MODULE
{
	name = ModuleAnimateActiveSecondary
	animationName = <string> // name of secondary active animation
	requireDeploy = <boolean> // only play if part is deployed. Can help save performance. False by default
	requireResource = <null> or <resourceName> // if a resource is given, stop animation when empty. Null by default.

	flameoutResource = <null> or <resourceName> // If a resource is given, use this as the controlling resource for the flameoutHideTransform, otherwise use the requireResource
	flameoutHideTransform = <null> or <transformName> // if animation stops due to requireResource, hide/disable all objects with this name, and their children. This accounts for animations of falling fluid. Not needed by animations of machinery cranking. Null by default.

	debug = false
}


ModuleAnimateGauge

A fuel gauge animator. It takes a named animation and sets the play position in accordance to the fill % of a resource that the part is holding.

MODULE
{
	name = ModuleAnimateGauge
	animationName = <string> // An animation.
	gaugeResource = <string> // The resource being measured.
	requireDeploy = <boolean> // False by default. If true then on deploy, plY the animation up from 0 at its own speed.

	debug = false
}
