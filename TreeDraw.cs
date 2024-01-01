using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PZKS;
using PZKS.MatrixSystem;
using PZKS.Parser;
using PZKS.Validation;

public partial class TreeDraw : Node2D
{
	private float _radius = 7.0f;
	private float _downStep = 50.0f;
	private Color _color = Colors.Yellow;
	private List<Label> _labels = new List<Label>();
	private List<Vector2> _circleCoordinates = new List<Vector2>();
	private List<(Vector2, Vector2)> _connectionCoordinates = new List<(Vector2, Vector2)>();

	private TextEdit _textEdit;
	private ExpressionNode TreeToDraw = null;
	private Lexer _lexer;
	private Parser _parser;
	private Balancer _balancer;
	private ValidatorStateMachine _validator;
	private Distributor _distributor;
	public void DrawATreeCallback()
	{
		var rawText = _textEdit.Text;
		var tokens = _lexer.Scan(rawText);
		_validator.Validate(tokens);
		var errors = Util.LogErrors();
		if (errors.Count > 0)
		{
			foreach (var error in errors)
			{
				GD.Print(error);
			}
			GD.Print("Errors were found, AST cannot be built");
			return;
		}
		
		var squashed = Util.TransformPreTree(tokens);
		TreeToDraw = _parser.CreateTree(squashed);
		_distributor.Distribute(ref TreeToDraw);   
		// var sysEval = new SystemEvaluator(TreeToDraw);
		// GD.Print(sysEval.GetAllStats());
		// var matrixSystem = sysEval.GetOptimalSystem();
		// GD.Print("====================================");
		// GD.Print("The best execution");
		// GD.Print("====================================");
		// GD.Print(matrixSystem.ToString());
		ConstructDrawingTree(TreeToDraw, -380, (Position.X * 2) + 380, Position);
	}
	
	private void ConstructDrawingTree(ExpressionNode node, float leftLimit, float rightLimit, Vector2 currentCoordinates)
	{
		if (node.Parent == null) // root
		{
			CleanupDrawing();
			CreateLabel(node, currentCoordinates);
			_circleCoordinates.Add(currentCoordinates);
		}
		
		float numberToDivide = node.Children.Count;
		var vectorAdd = (rightLimit - leftLimit) / (numberToDivide);
		for (int i = 0; i < node.Children.Count; i++)
		{
			var childNode = node.Children[i];
			var childLeftLimit = leftLimit + i * vectorAdd;
			var childRightLimit = leftLimit + (i + 1) * vectorAdd;
			
			var xCoord = (childLeftLimit + childRightLimit) * 0.5f;
			var yCoord = currentCoordinates.Y + _downStep;
			var newCoord = new Vector2(xCoord, yCoord);
			CreateLabel(childNode, newCoord);
			_connectionCoordinates.Add((currentCoordinates, newCoord));
			_circleCoordinates.Add(newCoord);
			if (childNode.Children.Count > 0)
			{
				ConstructDrawingTree(childNode, childLeftLimit, childRightLimit, newCoord);
			}
		}
	}

	public override void _Draw()
	{
		if (_connectionCoordinates.Count == 0 || _circleCoordinates.Count == 0)
		{
			return;
		}
		DrawConnections();
		DrawTreeCircles();
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		TreeToDraw = null;
		_lexer = new Lexer();
		_validator = new ValidatorStateMachine();
		var start = new StartValidator();
		var parentheses = new ParenthesesValidator();
		var operations = new OperationsValidator();
		var commas = new FunctionValidator();
		var end = new EndValidator();
        
		_validator.StartState = start;
		start.NextState = parentheses;
		parentheses.NextState = commas;
		commas.NextState = operations;
		operations.NextState = end;
		
		_parser = new Parser();
		_balancer = new Balancer();
		_distributor = new Distributor();
		
		_textEdit = (TextEdit)GetParent().GetNode("TextEdit");
	}

	void DrawTreeCircles()
	{
		foreach (var coordinates in _circleCoordinates)
		{
			DrawCircle(coordinates, _radius, _color);
		}
	}

	void DrawConnections()
	{
		foreach (var (start, end) in _connectionCoordinates)
		{
			DrawLine(start, end, Colors.Black);
		}
	}

	void CreateLabel(ExpressionNode node, Vector2 coordinates)
	{
		var l = new Label();
		l.Text = node.ToString();
		l.Position = coordinates;
		AddChild(l);
		_labels.Add(l);
	}

	private void CleanupDrawing()
	{
		foreach (var label in _labels)
		{
			label.QueueFree();
		}
		_labels.Clear();
		_circleCoordinates.Clear();
		_connectionCoordinates.Clear();
		QueueRedraw();
	}
	
	/*
	1) a+b+c+d+e+f+g+h
	2) a-b-c-d-e-f-g-h
	3) a+(b+c+d+(e+f)+g)+h
	4) a-((b-c-d)-(e-f)-g)-h
	5) a/b/c/d/e/f/g/h
	6) a*b - b*c - c*d - a*c*(b-d/e/f/g) - (g - h) - (i-j)
	7) 5040/8/7/6/5/4/3/2 (0.125)
	8) 10-9-8-7-6-5-4-3-2-1 (-35)
	9) 64-(32-16)-8-(4-2-1) (39)
	10) 3^3^3^3^3
	11) --i/1.0 + 0 - 0*k*h + 2 - 4.8/2 + 1*e/2
	12) a*2/0 + b/(b+b*0-1*b) - 1/(c*2*4.76*(1-2+1))
*/
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
}
