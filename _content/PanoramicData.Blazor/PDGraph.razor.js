/**
 * PDGraph.razor.js - Advanced force-directed graph layout with multi-dimensional positioning
 * Supports physics-based animations and dimensional projection
 */

class GraphRenderer {
	constructor(elementId, clusteringConfig) {
		this.clusteringConfig = clusteringConfig;
		this.elementId = elementId;
		this.svg = null;
		this.nodes = [];
		this.edges = [];
		this.dimensions = [];
		this.simulation = null;
		this.transform = { x: 0, y: 0, k: 1 };
		this.isDragging = false;
		this.isRunning = false;
		this.isAnimating = false;
		this.focusNode = null;

		// Force simulation parameters
		this.params = {
			repulsionStrength: 800,      // ✅ INCREASED: Much stronger repulsion to spread nodes
			attractionStrength: 0.05,    // ✅ DECREASED: Weaker attraction to allow spreading
			damping: 0.95,               // ✅ INCREASED: More damping for faster settling
			velocityDecay: 0.05,         // ✅ INCREASED: More decay to maintain movement
			minDistance: 80,             // ✅ INCREASED: Larger minimum distance between nodes
			maxDistance: 500,            // ✅ INCREASED: Larger max distance for forces
			iterations: 600,             // ✅ INCREASED: More iterations for longer physics
			convergenceThreshold: 0.02,  // ✅ MUCH LOWER: Allow physics to continue longer
			centerForce: 0.005,          // ✅ DECREASED: Weaker center force to allow spreading
			focusForce: 0.03             // ✅ DECREASED: Weaker focus force
		};

		// Animation parameters
		this.animationParams = {
			duration: 2000, // 2 seconds
			easing: 'easeInOutCubic'
		};
	}

	initialize() {
		const container = document.getElementById(this.elementId);
		if (!container) return;

		this.svg = container.querySelector('.graph-svg');
		if (!this.svg) return;

		this.setupEventListeners();
		console.log(`PDGraph ${this.elementId} initialized with advanced physics`);
	}

	setupEventListeners() {
		if (!this.svg) return;

		// ✅ FIXED: Proper method binding to avoid undefined errors
		// Mouse/touch interactions for pan and zoom
		this.svg.addEventListener('mousedown', (e) => this.onMouseDown(e));
		this.svg.addEventListener('mousemove', (e) => this.onMouseMove(e));
		this.svg.addEventListener('mouseup', (e) => this.onMouseUp(e));
		this.svg.addEventListener('wheel', (e) => this.onWheel(e));

		// Touch events for mobile
		this.svg.addEventListener('touchstart', (e) => this.onTouchStart(e));
		this.svg.addEventListener('touchmove', (e) => this.onTouchMove(e));
		this.svg.addEventListener('touchend', (e) => this.onTouchEnd(e));

		// Prevent context menu
		this.svg.addEventListener('contextmenu', e => e.preventDefault());
	}

	startForceSimulation(graphData) {
		if (!graphData || !graphData.nodes) return;

		// Get viewport dimensions for proper centering
		const rect = this.svg.getBoundingClientRect();
		const centerX = rect.width / 2;
		const centerY = rect.height / 2;

		// ✅ FIXED: Much larger spread considering viewport aspect ratio
		const radiusX = rect.width * 0.4; // ✅ INCREASED: 40% of width for better spread  
		const radiusY = rect.height * 0.4; // ✅ INCREASED: 40% of height for better spread
		const averageRadius = (radiusX + radiusY) / 2;

		// Check if this is initial load or just an update
		const isInitialLoad = this.nodes.length === 0;
		const needsRepositioning = this.nodes.length > 0 &&
			this.nodes.every(n => n.x === 0 && n.y === 0);

		if (isInitialLoad || needsRepositioning) {
			// Generate diverse initial positions using golden ratio spiral with proper spacing
			this.nodes = graphData.nodes.map((node, index) => {
				// Use golden ratio spiral for beautiful, diverse positioning
				const goldenRatio = (1 + Math.sqrt(5)) / 2;
				const angle = index * 2 * Math.PI / goldenRatio; // True golden angle

				// ✅ FIXED: Better distance progression with aspect ratio consideration
				const distance = Math.sqrt(index + 1) * 60; // Increased from 40 to 60 for more spread

				// ✅ FIXED: Apply aspect ratio to X and Y positions differently
				const normalizedDistance = Math.min(distance, averageRadius);
				const xDistance = normalizedDistance * (radiusX / averageRadius);
				const yDistance = normalizedDistance * (radiusY / averageRadius);

				// Add some randomness for more natural look but keep structure
				const randomAngleOffset = (Math.random() - 0.5) * 0.3; // Smaller angle variation
				const finalAngle = angle + randomAngleOffset;
				const randomDistanceOffset = (Math.random() - 0.5) * 30; // Distance variation

				return {
					id: node.id,
					label: node.label,
					x: centerX + Math.cos(finalAngle) * (xDistance + randomDistanceOffset),
					y: centerY + Math.sin(finalAngle) * (yDistance + randomDistanceOffset),
					vx: (Math.random() - 0.5) * 6, // ✅ Larger initial velocity for more movement
					vy: (Math.random() - 0.5) * 6, // ✅ Larger initial velocity for more movement  
					fx: node.isFixed ? node.x : null,
					fy: node.isFixed ? node.y : null,
					dimensions: node.dimensions || {},
					originalNode: node
				};
			});

			this.edges = (graphData.edges || []).map(edge => ({
				id: edge.id,
				source: edge.fromNodeId,
				target: edge.toNodeId,
				strength: edge.strength || 1.0,
				dimensions: edge.dimensions || {},
				originalEdge: edge
			}));

			// Extract unique dimensions for multi-dimensional calculations
			this.extractDimensions();

			console.log(`Generated initial positions for ${this.nodes.length} nodes with spread: ${radiusX.toFixed(0)}x${radiusY.toFixed(0)}`);
		} else {
			// Just update existing nodes with new data, preserve positions
			this.nodes.forEach(node => {
				const updatedNode = graphData.nodes.find(n => n.id === node.id);
				if (updatedNode) {
					node.dimensions = updatedNode.dimensions || {};
					node.originalNode = updatedNode;
					node.label = updatedNode.label;
				}
			});

			// Update edges too
			this.edges.forEach(edge => {
				const updatedEdge = graphData.edges.find(e => e.id === edge.id);
				if (updatedEdge) {
					edge.dimensions = updatedEdge.dimensions || {};
					edge.originalEdge = updatedEdge;
					edge.strength = updatedEdge.strength || 1.0;
				}
			});

			console.log(`Updated existing node data, preserved positions`);
		}

		this.runSimulation();
	}

	extractDimensions() {
		const dimensionSet = new Set();

		this.nodes.forEach(node => {
			Object.keys(node.dimensions).forEach(dim => dimensionSet.add(dim));
		});

		this.edges.forEach(edge => {
			Object.keys(edge.dimensions).forEach(dim => dimensionSet.add(dim));
		});

		this.dimensions = Array.from(dimensionSet);
		console.log('Extracted dimensions:', this.dimensions);
	}

	runSimulation() {
		if (this.isRunning) return;

		this.isRunning = true;
		let iteration = 0;
		const maxIterations = this.params.iterations;

		const tick = () => {
			if (iteration >= maxIterations || !this.isRunning) {
				this.isRunning = false;
				console.log(`Simulation completed after ${iteration} iterations`);
				return;
			}

			this.updateForces();
			this.updatePositions();
			this.updateDOM();

			// Check convergence
			const totalKineticEnergy = this.calculateKineticEnergy();
			if (iteration < 10 || iteration % 50 === 0) {
				console.log(`Iteration ${iteration}: Kinetic Energy = ${totalKineticEnergy.toFixed(3)}`);
			}

			if (totalKineticEnergy < this.params.convergenceThreshold) {
				this.isRunning = false;
				console.log(`Simulation converged after ${iteration} iterations (energy: ${totalKineticEnergy.toFixed(3)})`);
				return;
			}

			iteration++;
			requestAnimationFrame(tick);
		};

		requestAnimationFrame(tick);
	}

	updateForces() {
		const { repulsionStrength, attractionStrength, minDistance, maxDistance, centerForce, focusForce } = this.params;

		// Multi-dimensional repulsion forces
		for (let i = 0; i < this.nodes.length; i++) {
			for (let j = i + 1; j < this.nodes.length; j++) {
				const nodeA = this.nodes[i];
				const nodeB = this.nodes[j];

				const dx = nodeB.x - nodeA.x;
				const dy = nodeB.y - nodeA.y;
				const distance = Math.sqrt(dx * dx + dy * dy);

				if (isNaN(distance) || distance === 0) continue; // Skip if distance is invalid or zero

				// Stronger repulsion at very close distances (like a strong nuclear force)
				const effectiveRepulsionStrength = repulsionStrength * (distance < minDistance ? 5 : 1); // 5x stronger if overlapping

				if (distance > 0 && distance < maxDistance) {
					// Calculate dimensional similarity (closer in dimensional space = stronger repulsion)
					const dimensionalSimilarity = this.calculateDimensionalSimilarity(nodeA, nodeB);
					const adjustedRepulsion = effectiveRepulsionStrength * (1 + dimensionalSimilarity * 0.5);

					const force = adjustedRepulsion / (distance * distance + 10); // Add small constant to prevent singularities
					const fx = (dx / distance) * force;
					const fy = (dy / distance) * force;

					if (nodeA.fx === null && nodeA.fy === null) {
						nodeA.vx -= fx;
						nodeA.vy -= fy;
					}
					if (nodeB.fx === null && nodeB.fy === null) {
						nodeB.vx += fx;
						nodeB.vy += fy;
					}
				}
			}
		}

		// Enhanced attraction forces based on edge strength and dimensional similarity
		this.edges.forEach(edge => {
			const source = this.nodes.find(n => n.id === edge.source);
			const target = this.nodes.find(n => n.id === edge.target);

			if (!source || !target) return;

			const dx = target.x - source.x;
			const dy = target.y - source.y;
			const distance = Math.sqrt(dx * dx + dy * dy);

			// ✅ FIXED: Spring-like behavior with natural length
			const edgeDimensionalWeight = this.calculateEdgeDimensionalWeight(edge);
			const adjustedAttraction = attractionStrength * edge.strength * edgeDimensionalWeight;

			// ✅ IMPROVED: Natural length based on edge strength and viewport size
			const rect = this.svg.getBoundingClientRect();
			const baseLength = Math.min(rect.width, rect.height) * 0.15; // 15% of smaller viewport dimension
			const naturalLength = baseLength * (1 + edge.strength * 0.5); // Stronger edges can be slightly longer

			// Spring force: F = k * (current_length - natural_length)
			const displacement = distance - naturalLength;
			const springForce = adjustedAttraction * displacement;

			if (distance > 0) {
				const fx = (dx / distance) * springForce;
				const fy = (dy / distance) * springForce;

				if (source.fx === null && source.fy === null) {
					source.vx += fx;
					source.vy += fy;
				}
				if (target.fx === null && target.fy === null) {
					target.vx -= fx;
					target.vy -= fy;
				}
			}
		});

		// Add collision force to prevent node overlap
		for (let i = 0; i < this.nodes.length; i++) {
			for (let j = i + 1; j < this.nodes.length; j++) {
				const nodeA = this.nodes[i];
				const nodeB = this.nodes[j];
				const dx = nodeB.x - nodeA.x;
				const dy = nodeB.y - nodeA.y;
				const distance = Math.sqrt(dx * dx + dy * dy);
				const minDistance = (this.calculateNodeStyle(nodeA).size + this.calculateNodeStyle(nodeB).size) * 1.2; // 20% buffer

				if (distance < minDistance) {
					const overlap = minDistance - distance;
					const force = overlap * 0.5; // Stronger push
					const fx = (dx / distance) * force;
					const fy = (dy / distance) * force;

					if (nodeA.fx === null && nodeA.fy === null) {
						nodeA.vx -= fx;
						nodeA.vy -= fy;
					}
					if (nodeB.fx === null && nodeB.fy === null) {
						nodeB.vx += fx;
						nodeB.vy += fy;
					}
				}
			}
		}

		// Apply clustering force
		if (this.clusteringConfig && this.clusteringConfig.clusterByDimension) {
			const clusterDimension = this.clusteringConfig.clusterByDimension;
			const clusterPoints = {};

			// Find the center of each cluster
			this.nodes.forEach(node => {
				const clusterValue = node.dimensions[clusterDimension];
				if (clusterValue !== undefined) {
					if (!clusterPoints[clusterValue]) {
						clusterPoints[clusterValue] = { x: 0, y: 0, count: 0 };
					}
					clusterPoints[clusterValue].x += node.x;
					clusterPoints[clusterValue].y += node.y;
					clusterPoints[clusterValue].count++;
				}
			});

			Object.values(clusterPoints).forEach(cp => {
				cp.x /= cp.count;
				cp.y /= cp.count;
			});

			// Pull nodes towards their cluster center
			this.nodes.forEach(node => {
				const clusterValue = node.dimensions[clusterDimension];
				if (clusterValue !== undefined) {
					const clusterPoint = clusterPoints[clusterValue];
					const dx = clusterPoint.x - node.x;
					const dy = clusterPoint.y - node.y;
					node.vx += dx * 0.01; // Gentle pull
					node.vy += dy * 0.01;
				}
			});
		}

		// Apply clustering force
		if (this.clusteringConfig && this.clusteringConfig.isEnabled && this.clusteringConfig.clusterByDimension) {
			const clusterDimension = this.clusteringConfig.clusterByDimension;
			const clusterPoints = {};

			// Find the center of each cluster
			this.nodes.forEach(node => {
				const clusterValue = node.dimensions[clusterDimension];
				if (clusterValue !== undefined) {
					if (!clusterPoints[clusterValue]) {
						clusterPoints[clusterValue] = { x: 0, y: 0, count: 0 };
					}
					clusterPoints[clusterValue].x += node.x;
					clusterPoints[clusterValue].y += node.y;
					clusterPoints[clusterValue].count++;
				}
			});

			Object.values(clusterPoints).forEach(cp => {
				cp.x /= cp.count;
				cp.y /= cp.count;
			});

			// Pull nodes towards their cluster center
			this.nodes.forEach(node => {
				const clusterValue = node.dimensions[clusterDimension];
				if (clusterValue !== undefined) {
					const clusterPoint = clusterPoints[clusterValue];
					const dx = clusterPoint.x - node.x;
					const dy = clusterPoint.y - node.y;
					node.vx += dx * 0.01; // Gentle pull
					node.vy += dy * 0.01;
				}
			});
		}

		// Center force or focus force - ALWAYS use actual viewport center
		const rect = this.svg.getBoundingClientRect();
		const actualCenterX = rect.width / 2;
		const actualCenterY = rect.height / 2;
		const forceStrength = this.focusNode ? focusForce : centerForce;

		this.nodes.forEach(node => {
			if (node.fx === null && node.fy === null) {
				if (this.focusNode && node.id === this.focusNode.id) {
					// Strong force to center the focus node
					node.vx += (actualCenterX - node.x) * forceStrength * 3;
					node.vy += (actualCenterY - node.y) * forceStrength * 3;
				} else if (this.focusNode) {
					// Position other nodes based on dimensional proximity to focus node
					const proximity = this.calculateDimensionalSimilarity(node, this.focusNode);
					const angle = this.calculateDimensionalAngle(node, this.focusNode);
					const idealDistance = 100 + (1 - proximity) * 180; // Closer in dimensional space = physically closer

					const idealX = actualCenterX + Math.cos(angle) * idealDistance;
					const idealY = actualCenterY + Math.sin(angle) * idealDistance;

					node.vx += (idealX - node.x) * forceStrength * 1.5;
					node.vy += (idealY - node.y) * forceStrength * 1.5;
				} else {
					// Regular center force - center on actual viewport center
					node.vx += (actualCenterX - node.x) * forceStrength;
					node.vy += (actualCenterY - node.y) * forceStrength;
				}
			}
		});
	}

	calculateDimensionalSimilarity(nodeA, nodeB) {
		if (this.dimensions.length === 0) return 0;

		let similarity = 0;
		let count = 0;

		this.dimensions.forEach(dim => {
			const valueA = nodeA.dimensions[dim] || 0;
			const valueB = nodeB.dimensions[dim] || 0;
			similarity += 1 - Math.abs(valueA - valueB); // Higher similarity for closer values
			count++;
		});

		return count > 0 ? similarity / count : 0;
	}

	calculateEdgeDimensionalWeight(edge) {
		if (this.dimensions.length === 0) return 1;

		let weight = 1;
		this.dimensions.forEach(dim => {
			const value = edge.dimensions[dim] || 0.5;
			weight += value * 0.5; // Edge dimensions boost connection strength
		});

		return weight;
	}

	calculateDimensionalAngle(node, focusNode) {
		if (this.dimensions.length === 0) return Math.random() * Math.PI * 2;

		// Create a unique angle based on dimensional differences
		let angle = 0;
		this.dimensions.forEach((dim, index) => {
			const nodeValue = node.dimensions[dim] || 0;
			const focusValue = focusNode.dimensions[dim] || 0;
			const difference = nodeValue - focusValue;
			angle += difference * (index + 1) * Math.PI / 2;
		});

		return angle;
	}

	updatePositions() {
		const { damping, velocityDecay } = this.params;
		const rect = this.svg.getBoundingClientRect();

		this.nodes.forEach(node => {
			// ✅ FIXED: Validate and fix any NaN values
			if (isNaN(node.x) || isNaN(node.y)) {
				console.warn(`Fixing NaN position for node ${node.id}`);
				node.x = rect.width / 2;
				node.y = rect.height / 2;
			}
			if (isNaN(node.vx) || isNaN(node.vy)) {
				console.warn(`Fixing NaN velocity for node ${node.id}`);
				node.vx = 0;
				node.vy = 0;
			}

			if (node.fx !== null && node.fy !== null) {
				// Fixed nodes
				node.x = node.fx;
				node.y = node.fy;
				node.vx = 0;
				node.vy = 0;
			} else {
				// Apply velocity
				node.x += node.vx;
				node.y += node.vy;

				// Apply damping
				node.vx *= damping;
				node.vy *= damping;

				// Velocity decay to prevent oscillation
				const speed = Math.sqrt(node.vx * node.vx + node.vy * node.vy);
				if (speed > 0.01) {
					const decay = Math.min(velocityDecay, speed);
					const factor = Math.max(0, (speed - decay) / speed);
					node.vx *= factor;
					node.vy *= factor;
				}
			}

			// Keep nodes within viewport with padding
			// node.x = Math.max(padding, Math.min(rect.width - padding, node.x));
			// node.y = Math.max(padding, Math.min(rect.height - padding, node.y));

			// ✅ FIXED: Final validation after position updates
			if (isNaN(node.x) || isNaN(node.y)) {
				console.error(`Node ${node.id} still has NaN position after update, resetting`);
				node.x = rect.width / 2;
				node.y = rect.height / 2;
				node.vx = 0;
				node.vy = 0;
			}
		});
	}

	calculateKineticEnergy() {
		return this.nodes.reduce((total, node) => {
			return total + (node.vx * node.vx + node.vy * node.vy);
		}, 0);
	}

	updateDOM() {
		this.renderNodesSVG();
		this.renderEdgesSVG();
	}

	renderNodesSVG() {
		if (!this.svg) return;

		const nodesGroup = this.svg.querySelector('.nodes-group');
		if (!nodesGroup) return;

		// Clear existing nodes
		nodesGroup.innerHTML = '';

		// ✅ FIXED: Filter out invalid nodes before rendering
		const validNodes = this.nodes.filter(node => {
			const isValid = !isNaN(node.x) && !isNaN(node.y) && 
				node.x !== undefined && node.y !== undefined &&
				isFinite(node.x) && isFinite(node.y);
			if (!isValid) {
				console.warn(`Skipping invalid node: ${node.id}`, { x: node.x, y: node.y });
			}
			return isValid;
		});

		// Render each valid node directly as SVG with multi-dimensional styling
		validNodes.forEach(node => {
			const nodeGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
			nodeGroup.setAttribute('class', 'graph-node');
			nodeGroup.setAttribute('transform', `translate(${node.x},${node.y})`);
			nodeGroup.setAttribute('data-node-id', node.id);

			// ✅ FIXED: Calculate multi-dimensional styling
			const nodeStyle = this.calculateNodeStyle(node);

			// Create shape based on dimensional data
			const shape = this.createNodeShape(nodeStyle);

			// ✅ ADD click handler for node selection
			shape.setAttribute('style', 'cursor: pointer;');
			shape.addEventListener('click', (e) => {
				e.stopPropagation();
				this.handleNodeClick(node);
			});
			
			nodeGroup.appendChild(shape);

			// Create label with contrasting color
			if (node.label) {
				const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				text.setAttribute('text-anchor', 'middle');
				text.setAttribute('dy', '.35em');
				text.setAttribute('font-size', Math.max(8, Math.min(12, nodeStyle.size * 0.4)));
				text.setAttribute('fill', this.getContrastingTextColor(nodeStyle.fillColor));
				text.setAttribute('pointer-events', 'none');
				text.setAttribute('user-select', 'none');
				text.setAttribute('font-family', 'Arial, sans-serif');
				text.setAttribute('font-weight', '500');
				
				const truncatedLabel = this.getTruncatedLabel(node.label, nodeStyle.size);
				text.textContent = truncatedLabel;
				nodeGroup.appendChild(text);
			}

			nodesGroup.appendChild(nodeGroup);
		});
	}

	calculateNodeStyle(node) {
		// Default styling
		const style = {
			size: 20,
			fillColor: '#4a90e2',
			fillAlpha: 0.8,
			strokeColor: '#333333',
			strokeThickness: 2,
			strokeAlpha: 1.0,
			shape: 'circle' // circle, oval, diamond, octagon, square, rectangle
		};

		// Apply dimensional styling if available
		if (node.dimensions) {
			// Size based on 'Influence' dimension
			const influence = node.dimensions['Influence'] || 0.5;
			style.size = 15 + (influence * 20); // 15-35px range

			// Color based on 'Era' dimension (hue)
			const era = node.dimensions['Era'] || 0.5;
			const fame = node.dimensions['Fame'] || 0.5;
			const creativity = node.dimensions['Creativity'] || 0.5;

			const hue = era * 360; // 0-360 degrees
			const saturation = fame * 100; // 0-100%
			const luminance = 30 + (creativity * 40); // 30-70% range for good contrast

			style.fillColor = `hsl(${hue.toFixed(0)}, ${saturation.toFixed(0)}%, ${luminance.toFixed(0)}%)`;

			// Shape based on 'Category' dimension
			const category = node.dimensions['Category'] || 0.5;
			const shapeIndex = Math.floor(category * 6); // 0-5 range
			const shapes = ['circle', 'oval', 'diamond', 'octagon', 'square', 'rectangle'];
			style.shape = shapes[Math.min(shapeIndex, shapes.length - 1)];
		}

		return style;
	}

	createNodeShape(style) {
		switch (style.shape) {
			case 'circle':
				const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
				circle.setAttribute('r', style.size);
				circle.setAttribute('fill', style.fillColor);
				circle.setAttribute('fill-opacity', style.fillAlpha);
				circle.setAttribute('stroke', style.strokeColor);
				circle.setAttribute('stroke-width', style.strokeThickness);
				circle.setAttribute('stroke-opacity', style.strokeAlpha);
				return circle;

			case 'oval':
				const ellipse = document.createElementNS('http://www.w3.org/2000/svg', 'ellipse');
				ellipse.setAttribute('rx', style.size * 1.4);
				ellipse.setAttribute('ry', style.size * 0.8);
				ellipse.setAttribute('fill', style.fillColor);
				ellipse.setAttribute('fill-opacity', style.fillAlpha);
				ellipse.setAttribute('stroke', style.strokeColor);
				ellipse.setAttribute('stroke-width', style.strokeThickness);
				return ellipse;

			case 'diamond':
				const diamond = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
				const diamondPoints = `0,${-style.size} ${style.size},0 0,${style.size} ${-style.size},0`;
				diamond.setAttribute('points', diamondPoints);
				diamond.setAttribute('fill', style.fillColor);
				diamond.setAttribute('stroke', style.strokeColor);
				diamond.setAttribute('stroke-width', style.strokeThickness);
				return diamond;

			case 'square':
				const square = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
				square.setAttribute('x', -style.size);
				square.setAttribute('y', -style.size);
				square.setAttribute('width', style.size * 2);
				square.setAttribute('height', style.size * 2);
				square.setAttribute('fill', style.fillColor);
				square.setAttribute('stroke', style.strokeColor);
				square.setAttribute('stroke-width', style.strokeThickness);
				return square;

			case 'rectangle':
				const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
				rect.setAttribute('x', -style.size * 1.5);
				rect.setAttribute('y', -style.size);
				rect.setAttribute('width', style.size * 3);
				rect.setAttribute('height', style.size * 2);
				rect.setAttribute('fill', style.fillColor);
				rect.setAttribute('stroke', style.strokeColor);
				rect.setAttribute('stroke-width', style.strokeThickness);
				return rect;

			case 'octagon':
				const octagon = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
				const octSize = style.size;
				const octInner = octSize * 0.7;
				const octagonPoints = `${-octInner},${-octSize} ${octInner},${-octSize} ${octSize},${-octInner} ${octSize},${octInner} ${octInner},${octSize} ${-octInner},${octSize} ${-octSize},${octInner} ${-octSize},${-octInner}`;
				octagon.setAttribute('points', octagonPoints);
				octagon.setAttribute('fill', style.fillColor);
				octagon.setAttribute('stroke', style.strokeColor);
				octagon.setAttribute('stroke-width', style.strokeThickness);
				return octagon;

			default:
				// Fallback to circle
				return this.createNodeShape({ ...style, shape: 'circle' });
		}
	}

	getContrastingTextColor(backgroundColor) {
		// For HSL colors, parse the luminance value
		if (backgroundColor.startsWith('hsl(')) {
			const values = backgroundColor.replace('hsl(', '').replace(')', '').split(',');
			if (values.length >= 3) {
				const luminance = parseFloat(values[2].replace('%', '').trim());
				// If luminance > 50%, use dark text, otherwise use light text
				return luminance > 50 ? '#212529' : '#ffffff';
			}
		}
		// Default to white text
		return '#ffffff';
	}

	getTruncatedLabel(label, nodeSize) {
		// ? FIXED: Better truncation logic for smaller fonts and various node sizes
		const baseLength = Math.max(3, Math.floor(nodeSize / 3.5)); // More generous length calculation

		// Handle different node sizes more intelligently
		let maxLength;
		if (nodeSize < 20) {
			maxLength = Math.min(4, baseLength); // Very small nodes: max 4 chars
		} else if (nodeSize < 30) {
			maxLength = Math.min(8, baseLength); // Medium nodes: max 8 chars  
		} else {
			maxLength = Math.min(12, baseLength); // Large nodes: max 12 chars
		}

		if (label.length <= maxLength) {
			return label;
		}

		// Smart truncation - try to keep meaningful parts
		if (maxLength <= 4) {
			return label.substring(0, maxLength - 1) + '…';
		} else {
			// For longer labels, try to keep the beginning which is usually most meaningful
			return label.substring(0, maxLength - 1) + '…';
		}
	}

	// ? ADDED: Node click handler with Blazor interop
	handleNodeClick(node) {
		console.log(`Node clicked: ${node.label || node.id}`);

		// Notify Blazor component about node selection
		try {
			const component = document.getElementById(this.elementId);
			if (component && component.blazorComponent) {
				component.blazorComponent.invokeMethodAsync('OnNodeClickedFromJS', node.originalNode)
					.catch(error => console.error('Error invoking node click:', error));
			}
		} catch (error) {
			console.error('Error handling node click:', error);
		}
	}

	// ? ADDED: Missing mouse and touch event handlers
	onMouseDown(event,) {
		if (event.target.closest('.graph-node') || event.target.closest('.graph-edge')) {
			return; // Don't pan when clicking on nodes/edges
		}

		this.isDragging = true;
		this.lastMouseX = event.clientX;
		this.lastMouseY = event.clientY;
		this.svg.style.cursor = 'grabbing';
		event.preventDefault();
	}

	onMouseMove(event) {
		if (this.isDragging && !this.isAnimating) {
			const deltaX = event.clientX - this.lastMouseX;
			const deltaY = event.clientY - this.lastMouseY;

			this.transform.x += deltaX;
			this.transform.y += deltaY;

			this.updateTransform();

			this.lastMouseX = event.clientX;
			this.lastMouseY = event.clientY;
		}
	}

	onMouseUp(event) {
		this.isDragging = false;
		this.svg.style.cursor = 'grab';
	}

	onWheel(event) {
		if (this.isAnimating) return;
		event.preventDefault();

		const rect = this.svg.getBoundingClientRect();
		const mouseX = event.clientX - rect.left;
		const mouseY = event.clientY - rect.top;

		const scaleFactor = event.deltaY > 0 ? 0.9 : 1.1;
		const newScale = Math.max(0.1, Math.min(5, this.transform.k * scaleFactor));

		// Zoom towards mouse position
		this.transform.x = mouseX - (mouseX - this.transform.x) * (newScale / this.transform.k);
		this.transform.y = mouseY - (mouseY - this.transform.y) * (newScale / this.transform.k);
		this.transform.k = newScale;

		this.updateTransform();
	}

	onTouchStart(event) {
		if (event.touches.length === 1) {
			const touch = event.touches[0];
			this.onMouseDown({
				clientX: touch.clientX,
				clientY: touch.clientY,
				target: event.target,
				preventDefault: () => event.preventDefault()
			});
		}
	}

	onTouchMove(event) {
		if (event.touches.length === 1 && this.isDragging) {
			const touch = event.touches[0];
			this.onMouseMove({
				clientX: touch.clientX,
				clientY: touch.clientY
			});
		}
		event.preventDefault();
	}

	onTouchEnd(event) {
		this.onMouseUp(event);
	}

	updateTransform() {
		const transformString = `translate(${this.transform.x},${this.transform.y}) scale(${this.transform.k})`;

		// Update transform on node and edge groups
		const nodesGroup = this.svg.querySelector('.nodes-group');
		const edgesGroup = this.svg.querySelector('.edges-group');

		if (nodesGroup) nodesGroup.setAttribute('transform', transformString);
		if (edgesGroup) edgesGroup.setAttribute('transform', transformString);

		this.notifyTransformChange(transformString);
	}

	notifyTransformChange(transformString) {
		try {
			const component = document.getElementById(this.elementId);
			if (component && component.blazorComponent) {
				component.blazorComponent.invokeMethodAsync('UpdateTransform', transformString);
			}
		} catch (error) {
			// Ignore errors when updating transform
		}
	}

	renderEdgesSVG() {
		if (!this.svg) return;

		const edgesGroup = this.svg.querySelector('.edges-group');
		if (!edgesGroup) return;

		// Clear existing edges
		edgesGroup.innerHTML = '';

		// ✅ FIXED: Validate edge positions before rendering
		this.edges.forEach(edge => {
			const sourceNode = this.nodes.find(n => n.id === edge.source);
			const targetNode = this.nodes.find(n => n.id === edge.target);

			// ✅ FIXED: Validate both nodes exist and have valid positions
			if (sourceNode && targetNode && 
				!isNaN(sourceNode.x) && !isNaN(sourceNode.y) &&
				!isNaN(targetNode.x) && !isNaN(targetNode.y) &&
				isFinite(sourceNode.x) && isFinite(sourceNode.y) &&
				isFinite(targetNode.x) && isFinite(targetNode.y)) {
				
				const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
				line.setAttribute('class', 'graph-edge');
				line.setAttribute('x1', sourceNode.x);
				line.setAttribute('y1', sourceNode.y);
				line.setAttribute('x2', targetNode.x);
				line.setAttribute('y2', targetNode.y);

				// ? IMPROVED: Better edge styling for visibility
				line.setAttribute('stroke', '#888'); // Lighter color for better visibility
				line.setAttribute('stroke-width', Math.max(1, edge.strength * 2)); // Variable width based on strength
				line.setAttribute('stroke-opacity', Math.max(0.4, edge.strength * 0.6)); // Variable opacity
				line.setAttribute('data-edge-id', edge.id);

				// ? Add hover effects
				line.setAttribute('style', 'cursor: pointer; transition: stroke-opacity 0.2s ease;');
				line.addEventListener('mouseenter', () => {
					line.setAttribute('stroke-opacity', Math.min(1.0, (parseFloat(line.getAttribute('stroke-opacity')) || 0.4) + 0.3));
				});
				line.addEventListener('mouseleave', () => {
					line.setAttribute('stroke-opacity', Math.max(0.4, edge.strength * 0.6));
				});

				edgesGroup.appendChild(line);
			} else {
				console.warn(`Skipping invalid edge: ${edge.id}`, {
					source: sourceNode ? { x: sourceNode.x, y: sourceNode.y } : 'missing',
					target: targetNode ? { x: targetNode.x, y: targetNode.y } : 'missing'
				});
			}
		});
	}

	// ? ADDED: Missing focus and layout methods
	setFocusNode(nodeId) {
		const node = this.nodes.find(n => n.id === nodeId);
		if (!node) return;

		console.log(`Setting focus to node: ${node.label || nodeId}`);

		// Clear any previous fixed positions
		this.nodes.forEach(n => {
			n.fx = null;
			n.fy = null;
		});

		// Store current positions for animation
		const currentPositions = {};
		this.nodes.forEach(n => {
			currentPositions[n.id] = { x: n.x, y: n.y };
		});

		this.focusNode = node;

		// Calculate new target positions based on dimensional relationships
		const rect = this.svg.getBoundingClientRect();
		const centerX = rect.width / 2;
		const centerY = rect.height / 2;

		// Fix the selected node to the center
		this.focusNode.fx = centerX;
		this.focusNode.fy = centerY;
		this.focusNode.vx = 0;
		this.focusNode.vy = 0;

		const targetPositions = {};
		this.nodes.forEach(n => {
			if (n.id === this.focusNode.id) {
				// Focus node goes to center
				targetPositions[n.id] = { x: centerX, y: centerY };
			} else {
				// Other nodes positioned based on dimensional similarity
				const proximity = this.calculateDimensionalSimilarity(n, this.focusNode);
				const angle = this.calculateDimensionalAngle(n, this.focusNode);
				const idealDistance = 100 + (1 - proximity) * 180; // Closer in dimensional space = physically closer

				targetPositions[n.id] = {
					x: centerX + Math.cos(angle) * idealDistance,
					y: centerY + Math.sin(angle) * idealDistance
				};
			}
		});

		// Animate all nodes to their new positions
		this.animateNodesToPositions(currentPositions, targetPositions, 1500); // 1.5 second animation

		// Restart simulation with focus forces after animation
		setTimeout(() => {
			this.runSimulation();
		}, 1600); // Start simulation after animation completes

		// Smooth zoom and center on the focus node
		this.animateCenterOnNode(centerX, centerY);
	}

	// ? ADDED: Animation methods
	animateNodesToPositions(fromPositions, toPositions, duration) {
		const startTime = performance.now();

		const animateNodes = (currentTime) => {
			const elapsed = currentTime - startTime;
			const progress = Math.min(elapsed / duration, 1);

			// Cubic ease-in-out
			const eased = progress < 0.5
				? 4 * progress * progress * progress
				: 1 - Math.pow(-2 * progress + 2, 3) / 2;

			// Update all node positions
			this.nodes.forEach(node => {
				const fromPos = fromPositions[node.id];
				const toPos = toPositions[node.id];

				if (fromPos && toPos) {
					node.x = fromPos.x + (toPos.x - fromPos.x) * eased;
					node.y = fromPos.y + (toPos.y - fromPos.y) * eased;

					// Reset velocities during animation
					node.vx = 0;
					node.vy = 0;
				}
			});

			// Update DOM with new positions
			this.updateDOM();

			if (progress < 1) {
				requestAnimationFrame(animateNodes);
			}
		};

		requestAnimationFrame(animateNodes);
	}

	animateCenterOnNode(x, y) {
		const rect = this.svg.getBoundingClientRect();
		const targetCenterX = rect.width / 2;
		const targetCenterY = rect.height / 2;

		// Calculate target transform
		const targetScale = Math.min(1.5, this.transform.k * 1.2); // Zoom in slightly
		const targetX = targetCenterX - x * targetScale;
		const targetY = targetCenterY - y * targetScale;

		// Animate transform
		this.animateTransform(
			{ x: this.transform.x, y: this.transform.y, k: this.transform.k },
			{ x: targetX, y: targetY, k: targetScale },
			this.animationParams.duration
		);
	}

	animateTransform(from, to, duration) {
		if (this.isAnimating) return;

		this.isAnimating = true;
		const startTime = performance.now();

		const animate = (currentTime) => {
			const elapsed = currentTime - startTime;
			const progress = Math.min(elapsed / duration, 1);

			// Cubic ease-in-out
			const eased = progress < 0.5
				? 4 * progress * progress * progress
				: 1 - Math.pow(-2 * progress + 2, 3) / 2;

			// Interpolate transform values
			this.transform.x = from.x + (to.x - from.x) * eased;
			this.transform.y = from.y + (to.y - from.y) * eased;
			this.transform.k = from.k + (to.k - from.k) * eased;

			this.updateTransform();

			if (progress < 1) {
				requestAnimationFrame(animate);
			} else {
				this.isAnimating = false;
			}
		};

		requestAnimationFrame(animate);
	}

	centerOnNode(x, y) {
		this.animateCenterOnNode(x, y);
	}

	fitToView() {
		if (this.nodes.length === 0) return;

		// ? FIXED: Better padding and viewport utilization
		const padding = 60; // Reduced padding for better viewport usage
		const rect = this.svg.getBoundingClientRect();

		let minX = Infinity, maxX = -Infinity;
		let minY = Infinity, maxY = -Infinity;

		// ? IMPROVED: Include node radius in bounds calculation
		this.nodes.forEach(node => {
			const nodeStyle = this.calculateNodeStyle(node);
			const nodeRadius = nodeStyle.size;

			minX = Math.min(minX, node.x - nodeRadius);
			maxX = Math.max(maxX, node.x + nodeRadius);
			minY = Math.min(minY, node.y - nodeRadius);
			maxY = Math.max(maxY, node.y + nodeRadius);
		});

		const graphWidth = maxX - minX;
		const graphHeight = maxY - minY;

		if (graphWidth === 0 || graphHeight === 0) return;

		// ? FIXED: Better scaling calculation that uses more of the viewport
		const availableWidth = rect.width - padding * 2;
		const availableHeight = rect.height - padding * 2;

		const scaleX = availableWidth / graphWidth;
		const scaleY = availableHeight / graphHeight;
		const targetScale = Math.min(scaleX, scaleY, 1.5); // Allow up to 1.5x zoom

		// ? FIXED: Proper centering calculation
		const graphCenterX = (minX + maxX) / 2;
		const graphCenterY = (minY + maxY) / 2;
		const viewportCenterX = rect.width / 2;
		const viewportCenterY = rect.height / 2;

		// Calculate transform to center the graph
		const targetX = viewportCenterX - graphCenterX * targetScale;
		const targetY = viewportCenterY - graphCenterY * targetScale;

		console.log(`Fit to view: scale=${targetScale.toFixed(2)}, center=(${graphCenterX.toFixed(1)}, ${graphCenterY.toFixed(1)}), transform=(${targetX.toFixed(1)}, ${targetY.toFixed(1)})`);

		// Clear focus node when fitting to view
		this.focusNode = null;

		this.animateTransform(
			{ x: this.transform.x, y: this.transform.y, k: this.transform.k },
			{ x: targetX, y: targetY, k: targetScale },
			this.animationParams.duration
		);
	}

	stopSimulation() {
		this.isRunning = false;
	}

	destroy() {
		this.stopSimulation();
		this.isAnimating = false;
		// Clean up event listeners if needed
	}
}

/**
 * Global graph instance management and Blazor interop
 */
const graphInstances = new Map();

export function initialize(elementId, dotNetRef, clusteringConfig) {
	console.log(`Initializing PDGraph for element: ${elementId}`);
	const renderer = new GraphRenderer(elementId, clusteringConfig);
	renderer.initialize();

	// Store the Blazor component reference
	const container = document.getElementById(elementId);
	if (container && dotNetRef) {
		container.blazorComponent = dotNetRef;
	}

	graphInstances.set(elementId, renderer);
	console.log(`PDGraph initialized successfully for ${elementId}`);
}

export function startForceSimulation(elementId, graphData) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.startForceSimulation(graphData);
	}
}

export function regenerateLayout(elementId, graphData, convergenceThreshold = 0.02, clusteringConfig) {
	console.log(`regenerateLayout called for ${elementId}`, graphData);
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		// ✅ FIXED: Don't restart if simulation is already running
		if (renderer.isRunning) {
			console.log(`Simulation already running for ${elementId}, skipping regenerate`);
			return;
		}

		// Update convergence threshold
		renderer.params.convergenceThreshold = convergenceThreshold;
		renderer.clusteringConfig = clusteringConfig;

		console.log(`Found renderer for ${elementId}, clearing nodes and starting simulation with convergence: ${convergenceThreshold}`);
		// Force regeneration by clearing existing nodes
		renderer.nodes = [];
		renderer.edges = [];
		renderer.focusNode = null;
		renderer.startForceSimulation(graphData);
	} else {
		console.error(`No renderer found for elementId: ${elementId}`);
	}
}

export function setFocusNode(elementId, nodeId) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.setFocusNode(nodeId);
	}
}

export function centerOnNode(elementId, x, y) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.centerOnNode(x, y);
	}
}

export function fitToView(elementId) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.fitToView();
	}
}

export function destroy(elementId) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.destroy();
		graphInstances.delete(elementId);
	}
}

// Add this function to the JavaScript file
export function updatePhysicsParameters(elementId, convergenceThreshold, damping) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		const oldThreshold = renderer.params.convergenceThreshold;
		renderer.params.convergenceThreshold = convergenceThreshold;
		renderer.params.damping = damping;
		console.log(`Updated physics parameters for ${elementId}: Threshold ${oldThreshold} -> ${convergenceThreshold}, Damping ${renderer.params.damping} -> ${damping}`);
		
		// Don't restart simulation unless it's already stopped and threshold changed significantly
		if (!renderer.isRunning && (Math.abs(oldThreshold - convergenceThreshold) > 0.01 || Math.abs(renderer.params.damping - damping) > 0.01)) {
			console.log(`Restarting simulation with new physics parameters`);
			renderer.runSimulation();
		}
	}
}

// ✅ FIXED: Update the updateConfiguration function to properly handle updates
export function updateConfiguration(elementId, graphData, clusteringConfig) {
	console.log(`updateConfiguration called for ${elementId}`);
	const renderer = graphInstances.get(elementId);
	if (!renderer) {
		console.error(`No renderer found for elementId: ${elementId}`);
		return;
	}

	renderer.clusteringConfig = clusteringConfig; // Assign the new clustering config

	if (!graphData || !graphData.nodes) {
		console.warn(`No graph data provided for updateConfiguration`);
		return;
	}

	console.log(`Updating configuration with ${graphData.nodes.length} nodes and ${graphData.edges?.length || 0} edges`);

	// Update existing nodes data while preserving positions
	renderer.nodes.forEach(node => {
		const updatedNode = graphData.nodes.find(n => n.id === node.id);
		if (updatedNode) {
			// Only update the data properties, preserve all position and physics data
			node.dimensions = updatedNode.dimensions || {};
			node.originalNode = updatedNode;
			node.label = updatedNode.label;
			
			// CRITICAL: Don't touch position or velocity data
			// Keep existing: node.x, node.y, node.vx, node.vy, node.fx, node.fy
		}
	});

	// Update existing edges
	renderer.edges.forEach(edge => {
		const updatedEdge = graphData.edges.find(e => e.id === edge.id);
		if (updatedEdge) {
			// Only update the data properties
			edge.dimensions = updatedEdge.dimensions || {};
			edge.originalEdge = updatedEdge;
			edge.strength = updatedEdge.strength || 1.0;
		}
	});

	// Extract dimensions again in case they changed
	renderer.extractDimensions();

	// Re-render with new styling but preserve positions
	renderer.updateDOM();

	// If clustering configuration changed, restart simulation to apply new forces
	if (JSON.stringify(renderer.clusteringConfig) !== JSON.stringify(clusteringConfig)) {
		console.log(`Clustering configuration changed, restarting simulation for ${elementId}`);
		renderer.clusteringConfig = clusteringConfig; // Ensure it's updated before restarting
		renderer.runSimulation();
	}

	console.log(`Configuration updated for ${elementId}, ${renderer.nodes.length} nodes, positions preserved`);
}

// Add this function to update selection styling without regenerating layout
export function updateSelection(elementId, selectedId, selectionType) {
	const renderer = graphInstances.get(elementId);
	if (!renderer) return;

	console.log(`Updating selection: ${selectionType} ${selectedId}`);

	// Clear all existing selections
	const nodesGroup = renderer.svg.querySelector('.nodes-group');
	const edgesGroup = renderer.svg.querySelector('.edges-group');

	if (nodesGroup) {
		nodesGroup.querySelectorAll('.graph-node').forEach(node => {
			node.classList.remove('selected');
			const shapes = node.querySelectorAll('circle, rect, ellipse, polygon');
			shapes.forEach(shape => {
				// Remove selection styling
				shape.removeAttribute('filter');
				const strokeWidth = parseFloat(shape.getAttribute('stroke-width')) || 2;
				if (strokeWidth > 4) { // If it was previously selected
					shape.setAttribute('stroke-width', strokeWidth - 2);
				}
				if (shape.getAttribute('stroke') === '#ffff00') {
					shape.setAttribute('stroke', shape.getAttribute('data-original-stroke') || '#333333');
				}
			});
		});
	}

	if (edgesGroup) {
		edgesGroup.querySelectorAll('.graph-edge').forEach(edge => {
			edge.classList.remove('selected');
			// Remove edge selection styling if needed
		});
	}

	// Apply selection to the specified element
	if (selectionType === 'node') {
		const selectedNode = nodesGroup?.querySelector(`[data-node-id="${selectedId}"]`);
		if (selectedNode) {
			selectedNode.classList.add('selected');
			const shapes = selectedNode.querySelectorAll('circle, rect, ellipse, polygon');
			shapes.forEach(shape => {
				// Store original stroke color
				const originalStroke = shape.getAttribute('stroke') || '#333333';
				shape.setAttribute('data-original-stroke', originalStroke);

				// Apply selection styling
				shape.setAttribute('stroke', '#ffff00'); // Yellow selection border
				const currentWidth = parseFloat(shape.getAttribute('stroke-width')) || 2;
				shape.setAttribute('stroke-width', currentWidth + 2);
				shape.setAttribute('filter', 'drop-shadow(0 0 8px rgba(255, 255, 0, 0.6))'); // Glow effect
			});
		}
	} else if (selectionType === 'edge') {
		const selectedEdge = edgesGroup?.querySelector(`[data-edge-id="${selectedId}"]`);
		if (selectedEdge) {
			selectedEdge.classList.add('selected');
			// Apply edge selection styling
			selectedEdge.setAttribute('stroke', '#ffff00');
			selectedEdge.setAttribute('stroke-width', '3');
		}
	}
}