from os import path


def triangulate(polygon):
    return [(polygon[0], polygon[i], polygon[i + 1]) for i in range(1, len(polygon) - 1)]


def read_obj(obj_path):
    vertices, normals, polygons = [], [], []

    with open(obj_path, "r") as file:
        for line in file:
            parts = line.strip().split()
            if parts[0] == "v":
                vertices.append(tuple(map(float, parts[1:4])))
            elif parts[0] == "vn":
                normals.append(tuple(map(float, parts[1:4])))
            elif parts[0] == "f":
                normal = int(parts[1].split("/")[2]) - 1
                polygon = [int(index.split("/")[0]) - 1 for index in parts[1:]]
                polygons.extend([(normal, tri) for tri in triangulate(polygon)])
    return vertices, normals, polygons


def write_stl(stl_path, vertices, normals, polygons):
    with open(stl_path, "w") as file:
        file.write("solid obj_to_stl\n")
        for normal, polygon in polygons:
            file.write(f"facet normal {normals[normal][0]} {normals[normal][1]} {normals[normal][2]}\n")
            file.write(" outer loop\n")
            for index in polygon:
                file.write(f"  vertex {vertices[index][0]} {vertices[index][1]} {vertices[index][2]}\n")
            file.write(" endloop\n")
            file.write("endfacet\n")
        file.write("endsolid obj_to_stl\n")


def obj2stl(obj_path, stl_path):
    vertices, normals, polygons = read_obj(obj_path)
    write_stl(stl_path, vertices, normals, polygons)
    print(f"success: {stl_path}")


if __name__ == "__main__":
    while True:
        input_path = input("obj path: ")
        if input_path == "" or not path.isfile(input_path):
            print("file not found")
        else:
            break

    while True:
        output_path = input("stl path: ")
        if output_path == "":
            print("specify directory or type '//' to output the same name ")
        elif output_path == "//":
            output_path = input_path.rpartition(".")[0] + ".stl"
            break
        else:
            break

    obj2stl(obj_path=input_path, stl_path=output_path)
