/*
 * Thank you for downloading the FFDevelopment Unity Package. This
 * package contains a few useful syste
 * /component/classes which I
 * feel are paramount to rapid development in Unity3d.
 *  
 * Contains:
 * 
 * FFAction:
 * This is a robust action system which can be used to setup logic
 * which happens over time, instantly, in parralell, or in sequence.
 * This is useful for a creating a lot of different behavior but
 * might initially be considered for stage-based AI, Controller
 * behavior, Enemy Behaviors, and general alteration of data over
 * time.
 * 
 * FFMessage/FFMessageBox/FFMessageBoard:
 * -FFMessage is a global message system which is easy to connect,
 * disconnect, and send. It is simple, flexable, and performanant
 * for any global events.
 * -FFMessageBox is basically identical with
 * the main difference being that it is a class which can be added
 * to a component for local events/messaging.
 * -FFMessageBoard is a entry-based eventsystem with gameobject
 * locallity implimented. This allows events to be sent to object
 * which then specific scripts may listen to.
 * 
 * FFPath:
 * FFPath is a basic path component which has no build-in controller
 * and a simple interface. It can be used to create DynamicPaths
 * which can alter the number of points and their position
 * while in-game. Lots of stuff which can be complished with this,
 * be creative you will see...
 * 
 * FFComponent:
 * FFComponent is a wrapper for Monobehavior and is useful
 * because it adds a getter for FFAction and adds FFPosition and
 * FFScale to pass to FFAction for changing the position of
 * an object over time.
 *  
 * FFMeta:
 * This contains FFVar<> and FFRef<> which are used to create
 * what some other languages call a Property Delegate which is
 * then used by FFAction. These types may be re-used whenever
 * useful for other system, but I would recomend a full
 * investigation of the class before-hand. To do most of your
 * work with FFAction you shouldn't need to use this because
 * of FFComponent's wrapper on transform.position/scale
 * 
 * FFVector:
 * A few data fields to allow for serialized types of Unity types.
 * 
 * 
 * FFMessageSystem:
 * The Central system which all FFMessage/FFMessageBoards use to
 * connect to the net. This is can also be used to tracks stats
 * of every event passed in your game.
 * 
 * FFSystem:
 * Holds a bunch of data and functionaity used by other systems
 * including time and netids. It is also responsible for disbatching
 * messages from the net.
 * 
 * Notice: This package is provided as is without any warrenty. I am
 * not liable for any damages or cost resulting from the use of this
 * package.
 * 
 * ******************************************************************
 * |                            Credits                             |
 * |                                                                |
 * |                                                                |
 * |I want to thank the Zero team at DigiPen Institute of Technology|
 * |for inspiring me to write this package which emulates some of   |
 * |the functionality in the Zero engine.                           |
 * |                                                                |
 * |                                                                |
 * |                           Creator                              |
 * |                                                                |
 * |                          Micah Rust                            |
 * |                                                                |
 * |                                                                |
 * ******************************************************************
 * 
 */

