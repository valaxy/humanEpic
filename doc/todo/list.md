完善flowtool的功能：
- main.tscn：主入口，负责管理主要布局，按照左边是scopePanel，中间是canvas，右边是unassignedPoolPanel进行布局设计，相关逻辑在FlowToolController里，直接引用即可
- Main.cs：挂载在main.tscn上，负责连接几个组件实现整体功能
- canvas.tscn：主要负责画布区域，可通过canvasView.tscn来实现
- CanvasController.cs：主要负责画布的控制逻辑，通过CanvasView实现画布渲染和交互
- 主要功能：
    * 当scopePanel切换scope时，画布更新节点的结构
    * 支持通过鼠标滚轮来放大或缩小画布
    * 默认情况所有的节点都保存在
