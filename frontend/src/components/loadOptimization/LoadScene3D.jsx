import { useEffect, useRef } from "react";
import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";

const PALETTE = [
    0x3b82f6,
    0x14b8a6,
    0xf59e0b,
    0x8b5cf6,
    0xec4899,
    0x06b6d4,
    0x84cc16,
    0xf97316
];

function LoadScene3D({ response }) {
    const mountRef = useRef(null);

    useEffect(() => {
        const mount = mountRef.current;
        if (!mount || !response?.vehicle) return;

        const space = response.vehicle;

        const length = Number(space.lengthCm);
        const width = Number(space.widthCm);
        const height = Number(space.heightCm);

        if (![length, width, height].every(
            value => Number.isFinite(value) && value > 0
        )) return;

        const scene = new THREE.Scene();
        scene.background = new THREE.Color(0x0b1220);

        const camera = new THREE.PerspectiveCamera(
            45,
            mount.clientWidth / Math.max(mount.clientHeight, 1),
            0.1,
            100000
        );

        const maxDimension = Math.max(length, width, height);
        const cameraDistance = maxDimension * 1.65;

        camera.position.set(
            cameraDistance,
            cameraDistance * 0.85,
            cameraDistance
        );

        const renderer = new THREE.WebGLRenderer({
            antialias: true,
            alpha: false
        });

        renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
        renderer.setSize(mount.clientWidth, mount.clientHeight);
        renderer.outputColorSpace = THREE.SRGBColorSpace;
        renderer.shadowMap.enabled = true;
        renderer.shadowMap.type = THREE.PCFSoftShadowMap;

        mount.appendChild(renderer.domElement);

        const controls = new OrbitControls(camera, renderer.domElement);
        controls.enableDamping = true;
        controls.dampingFactor = 0.06;
        controls.target.set(length / 2, height / 2, width / 2);
        controls.minDistance = maxDimension * 0.25;
        controls.maxDistance = maxDimension * 5;

        scene.add(new THREE.HemisphereLight(0xffffff, 0x263449, 2));

        const keyLight = new THREE.DirectionalLight(0xffffff, 2.2);
        keyLight.position.set(length, height * 2, width);
        keyLight.castShadow = true;
        scene.add(keyLight);

        const fillLight = new THREE.DirectionalLight(0x8bbcff, 0.8);
        fillLight.position.set(-length, height, -width);
        scene.add(fillLight);

        // Truck floor
        const floorGeometry = new THREE.BoxGeometry(
            length,
            Math.max(maxDimension * 0.003, 0.5),
            width
        );

        const floorMaterial = new THREE.MeshStandardMaterial({
            color: 0x263449,
            roughness: 0.82,
            metalness: 0.15
        });

        const floor = new THREE.Mesh(floorGeometry, floorMaterial);
        floor.position.set(
            length / 2,
            -Math.max(maxDimension * 0.003, 0.5) / 2,
            width / 2
        );
        floor.receiveShadow = true;
        scene.add(floor);

        // Interior wireframe
        const containerGeometry = new THREE.BoxGeometry(
            length,
            height,
            width
        );

        const containerEdges = new THREE.EdgesGeometry(containerGeometry);
        const containerLines = new THREE.LineSegments(
            containerEdges,
            new THREE.LineBasicMaterial({
                color: 0x8ba3bd,
                transparent: true,
                opacity: 0.85
            })
        );

        containerLines.position.set(
            length / 2,
            height / 2,
            width / 2
        );

        scene.add(containerLines);

        // Ground grid
        const gridSize = Math.max(length, width);
        const grid = new THREE.GridHelper(
            gridSize,
            Math.max(2, Math.min(40, Math.round(gridSize / 20))),
            0x52657d,
            0x27384d
        );

        grid.position.set(length / 2, -0.8, width / 2);
        scene.add(grid);

        // Cargo packages
        const placements = response.placements || [];

        placements.forEach((item, index) => {
            const boxLength = Number(item.lengthCm);
            const boxWidth = Number(item.widthCm);
            const boxHeight = Number(item.heightCm);

            const x = Number(item.x);
            const y = Number(item.y);
            const z = Number(item.z);

            if (![boxLength, boxWidth, boxHeight, x, y, z].every(
                Number.isFinite
            )) return;

            const geometry = new THREE.BoxGeometry(
                boxLength,
                boxHeight,
                boxWidth
            );

            const color = PALETTE[index % PALETTE.length];

            const material = new THREE.MeshStandardMaterial({
                color,
                roughness: 0.56,
                metalness: 0.08
            });

            const packageMesh = new THREE.Mesh(geometry, material);

            // Backend coordinates: X=length, Y=width, Z=height.
            // Three.js coordinates: X=length, Y=height, Z=width.
            packageMesh.position.set(
                x + boxLength / 2,
                z + boxHeight / 2,
                y + boxWidth / 2
            );

            packageMesh.castShadow = true;
            packageMesh.receiveShadow = true;

            packageMesh.userData = {
                packageId: item.packageId,
                unitNumber: item.unitNumber,
                fragile: item.isFragile
            };

            scene.add(packageMesh);

            const edges = new THREE.EdgesGeometry(geometry);
            const outline = new THREE.LineSegments(
                edges,
                new THREE.LineBasicMaterial({
                    color: 0xffffff,
                    transparent: true,
                    opacity: 0.75
                })
            );

            outline.position.copy(packageMesh.position);
            scene.add(outline);

            if (item.isFragile) {
                const fragileEdges = new THREE.LineSegments(
                    new THREE.EdgesGeometry(geometry),
                    new THREE.LineBasicMaterial({
                        color: 0xff4d6d,
                        linewidth: 2
                    })
                );

                fragileEdges.position.copy(packageMesh.position);
                scene.add(fragileEdges);
            }
        });

        // Camera framing
        controls.update();

        let animationFrame;

        const animate = () => {
            animationFrame = requestAnimationFrame(animate);
            controls.update();
            renderer.render(scene, camera);
        };

        animate();

        const resizeObserver = new ResizeObserver(() => {
            const w = mount.clientWidth;
            const h = mount.clientHeight;

            if (w <= 0 || h <= 0) return;

            camera.aspect = w / h;
            camera.updateProjectionMatrix();
            renderer.setSize(w, h);
        });

        resizeObserver.observe(mount);

        return () => {
            cancelAnimationFrame(animationFrame);
            resizeObserver.disconnect();
            controls.dispose();

            scene.traverse(object => {
                if (object.geometry) object.geometry.dispose();

                if (object.material) {
                    const materials = Array.isArray(object.material)
                        ? object.material
                        : [object.material];

                    materials.forEach(material => material.dispose());
                }
            });

            renderer.dispose();

            if (renderer.domElement.parentNode === mount) {
                mount.removeChild(renderer.domElement);
            }
        };
    }, [response]);

    if (!response?.vehicle) {
        return (
            <div className="load-scene-empty">
                <div className="load-scene-empty-icon">▧</div>
                <h3>3D cargo visualization</h3>
                <p>Run optimization to see the calculated package arrangement.</p>
            </div>
        );
    }

    return (
        <div className="load-scene-wrapper">
            <div className="load-scene-toolbar">
                <span><i className="scene-dot" /> Vehicle interior</span>
                <span>Drag to rotate · Scroll to zoom</span>
            </div>

            <div className="load-scene-canvas" ref={mountRef} />

            <div className="load-scene-legend">
                <span><i className="legend-box" /> Placed cargo</span>
                <span><i className="legend-fragile" /> Fragile package</span>
                <span><i className="legend-outline" /> Vehicle boundary</span>
            </div>
        </div>
    );
}

export default LoadScene3D;
